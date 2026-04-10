const CACHE_NAME = 'grimdark-cache-v2.1-android';
const urlsToCache = [
  './index.html',
  './app.js',
  './data.js',
  './wh40k_logo.png',
  'https://cdnjs.cloudflare.com/ajax/libs/jszip/3.10.1/jszip.min.js'
];

self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(urlsToCache))
  );
});

self.addEventListener('fetch', event => {
  event.respondWith(
    caches.match(event.request)
      .then(response => {
        if (response) return response;
        return fetch(event.request);
      }
    )
  );
});
