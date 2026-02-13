// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).

self.addEventListener('fetch', () => { });


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