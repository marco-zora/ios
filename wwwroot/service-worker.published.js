// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

/*
async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache,
        // unless that request is for an offline resource.
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.some(url => url === event.request.url);

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    return cachedResponse || fetch(event.request);
}
*/

// Esempio: service-worker.js Network FIRST con fallback cache per navigazioni SPA (index.html) e risorse offline (manifest)

const cacheName = 'app-cache-v1';
// manifestUrlList: array di asset precache (iniezione build Blazor)
// Esempio: const manifestUrlList = self.assetsManifest?.assets?.map(a => new URL(a.url, self.location).href) ?? [];

self.addEventListener('fetch', event => {
    // Gestiamo solo richieste GET; per il resto lasciamo passare
    if (event.request.method !== 'GET') {
        return;
    }

    event.respondWith(onFetchNetworkFirst(event));
});

async function onFetchNetworkFirst(event) {
    const req = event.request;

    // Rileva le navigazioni (SPA) che NON puntano a risorse del manifest
    const isNavigation = req.mode === 'navigate';
    const isOfflineResource = manifestUrlList?.some(url => url === req.url) === true;

    // Per le navigazioni, la risorsa "logica" da usare come fallback cache è index.html
    const cacheKeyForNav = 'index.html';

    try {
        // 1) Prova la rete prima di tutto
        const networkResponse = await fetch(req);

        // 2) Se risposta ok/opaque: metti in cache (solo GET)
        if (networkResponse && (networkResponse.ok || networkResponse.type === 'opaque')) {
            // Apri cache e clona risposta
            const cache = await caches.open(cacheName);
            // Per le navigazioni, ha senso mettere in cache anche index.html
            if (isNavigation && !isOfflineResource) {
                cache.put(cacheKeyForNav, networkResponse.clone());
            } else {
                cache.put(req, networkResponse.clone());
            }
        }

        // 3) Ritorna sempre la risposta di rete (Network First)
        return networkResponse;
    } catch (err) {
        // 4) Se la rete fallisce, fallback sulla cache
        const cache = await caches.open(cacheName);

        if (isNavigation && !isOfflineResource) {
            // Navigazione offline -> prova index.html
            const cachedIndex = await cache.match(cacheKeyForNav);
            if (cachedIndex) return cachedIndex;
        }

        // Altrimenti prova a recuperare la risorsa richiesta dalla cache
        const cached = await cache.match(req);
        if (cached) return cached;

        // Ultimo fallback: una Response generica o un 504
        return new Response('Offline e risorsa non in cache.', { status: 504, statusText: 'Gateway Timeout' });
    }
}

