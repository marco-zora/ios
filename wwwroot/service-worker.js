// -------------------------------------------------------------
// Blazor PWA - SERVICE WORKER (Network First per TUTTE le API)
// -------------------------------------------------------------
const SW_VERSION = 'v2026-02-16-01';

// Elenco delle basi da NON cache-are (API, Proxy, Worker, WebDAV)
const API_BASES = [    
    'https://first.marcomaria-zora.workers.dev/api/dav/UPLOADS/',
    'http://localhost:7004/'                     // esempio locale
];

// Cache degli asset statici (non delle API!)
const STATIC_CACHE = `static-${SW_VERSION}`;
const STATIC_ASSETS = [
    '/', '/index.html', '/manifest.json',
    '/css/app.css', '/js/app.js'
];

// Installazione
self.addEventListener('install', event => {
    self.skipWaiting();
    event.waitUntil(
        caches.open(STATIC_CACHE).then(cache => cache.addAll(STATIC_ASSETS))
    );
});

// Attivazione
self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        // Prende subito il controllo
        await clients.claim();

        // Rimuove vecchie cache
        const keys = await caches.keys();
        await Promise.all(keys
            .filter(k => k !== STATIC_CACHE)
            .map(k => caches.delete(k))
        );
    })());
});

// Controllo se un URL rientra nelle API
function isApiRequest(requestUrl) {
    try {
        const url = new URL(requestUrl);
        return API_BASES.some(base => requestUrl.startsWith(base));
    } catch { return false; }
}

// FETCH HANDLER
self.addEventListener('fetch', event => {
    const req = event.request;

    // ❗ Non toccare la preflight OPTIONS (lascia al browser)
    if (req.method === 'OPTIONS') return;

    const url = req.url;

    // -----------------------------------------------------
    //   1) API / Proxy / WebDAV -> NETWORK FIRST (no cache)
    // -----------------------------------------------------
    if (isApiRequest(url)) {
        event.respondWith(
            fetch(req)
                .then(resp => resp)
                .catch(() => {
                    // Fallback in caso di rete non disponibile
                    return new Response(
                        JSON.stringify({ error: 'Offline', detail: 'Network unavailable' }),
                        { status: 503, headers: { 'Content-Type': 'application/json' } }
                    );
                })
        );
        return;
    }

    // -----------------------------------------------------
    //   2) Navigazioni → Network First con fallback cache
    // -----------------------------------------------------
    if (req.mode === 'navigate') {
        event.respondWith(
            fetch(req)
                .then(resp => resp)
                .catch(async () => {
                    const cache = await caches.open(STATIC_CACHE);
                    return await cache.match('/index.html');
                })
        );
        return;
    }

    // -----------------------------------------------------
    //   3) Asset statici → Cache First
    // -----------------------------------------------------
    if (req.destination === 'style' || req.destination === 'script' || req.destination === 'image' || req.destination === 'font') {
        event.respondWith((async () => {
            const cache = await caches.open(STATIC_CACHE);
            const cached = await cache.match(req);
            if (cached) return cached;

            try {
                const fresh = await fetch(req);
                if (fresh.ok) cache.put(req, fresh.clone());
                return fresh;
            } catch {
                return cached || Response.error();
            }
        })());
        return;
    }

    // -----------------------------------------------------
    //   4) Default → Network first con fallback 502
    // -----------------------------------------------------
    event.respondWith(
        fetch(req).catch(() => new Response(null, { status: 502 }))
    );
});



































// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).

//self.addEventListener('fetch', () => { });


// Network First: tenta di recuperare la risorsa dalla rete, se fallisce (ad esempio, se l'utente è offline), allora cerca nella cache.
// aggiorna la cache con la risposta più recente dalla rete, in modo che la prossima volta che l'utente accede alla risorsa, possa essere servita dalla cache se la rete non è disponibile.
/*
self.addEventListener('fetch',
    function (event) {
        event.respondWith(
            fetch(event.request).then(
                function (response) {
                    cache.put(event.request, response.clone());
                    return response;
                }).catch(function () {
                    return caches.match(event.request);
                })
        );
    });
    */

//Questo fa due cose:
//skipWaiting() → installa subito la nuova versione
//clients.claim() → ricarica immediatamente le pagine aperte
//Risultato: alla prossima pubblicazione, la PWA si aggiornerà da sola.
//va copiato anche alla fine del file service-worker.published.js

// Notifica alla pagina che c'è un nuovo service worker pronto
/*
self.addEventListener('install', () => {
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});
/*
// Quando arriva una nuova versione, invia un messaggio alle pagine aperte
self.addEventListener('message', event => {
    if (event.data === 'CHECK_FOR_UPDATE') {
        self.skipWaiting();
    }
});
*/