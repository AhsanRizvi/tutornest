const CACHE_NAME = 'tutornest-pwa-cache-v1';
const ASSETS_TO_CACHE = [
  '/',
  '/index.html',
  '/favicon.ico',
  '/manifest.webmanifest'
];

// Install Event
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME).then((cache) => {
      return cache.addAll(ASSETS_TO_CACHE);
    })
  );
});

// Activate Event
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((keys) => {
      return Promise.all(
        keys.map((key) => {
          if (key !== CACHE_NAME) {
            return caches.delete(key);
          }
        })
      );
    })
  );
});

// Fetch Event (Offline Support)
self.addEventListener('fetch', (event) => {
  event.respondWith(
    caches.match(event.request).then((cachedResponse) => {
      return cachedResponse || fetch(event.request);
    })
  );
});

// Push Event
self.addEventListener('push', (event) => {
  let data = { title: 'TutorNest Alert', body: 'New update available!' };
  if (event.data) {
    try {
      data = event.data.json();
    } catch (e) {
      data = { title: 'TutorNest Alert', body: event.data.text() };
    }
  }

  const options = {
    body: data.body,
    icon: '/favicon.ico',
    badge: '/favicon.ico',
    data: data
  };

  event.waitUntil(
    self.registration.showNotification(data.title, options)
  );
});
