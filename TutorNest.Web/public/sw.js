const CACHE_NAME = 'tutornest-pwa-cache-v1';
const ASSETS_TO_CACHE = [
  '/',
  '/index.html',
  '/favicon.ico',
  '/manifest.webmanifest'
];

// Install Event
self.addEventListener('install', (event) => {
  self.skipWaiting();
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
    }).then(() => self.clients.claim())
  );
});

// Fetch Event (Network-First Strategy for SPA navigation & documents, Cache-First for others)
self.addEventListener('fetch', (event) => {
  // Bypass non-GET requests and dev server websocket / hot reload scripts
  if (event.request.method !== 'GET' || 
      event.request.url.includes('hot-update') || 
      event.request.url.includes('ws') || 
      event.request.url.includes('sockjs')) {
    return;
  }

  const isNavigation = event.request.mode === 'navigate' || 
                       event.request.url.endsWith('/') || 
                       event.request.url.endsWith('index.html');

  if (isNavigation) {
    // Network-First for navigation and index.html to ensure we get new hashes
    event.respondWith(
      fetch(event.request)
        .then((response) => {
          // Cache the fresh response
          const responseClone = response.clone();
          caches.open(CACHE_NAME).then((cache) => {
            cache.put(event.request, responseClone);
          });
          return response;
        })
        .catch(() => {
          // Offline fallback
          return caches.match(event.request) || caches.match('/index.html');
        })
    );
  } else {
    // Cache-First with Network fallback for static assets
    event.respondWith(
      caches.match(event.request).then((cachedResponse) => {
        if (cachedResponse) {
          return cachedResponse;
        }
        return fetch(event.request).then((response) => {
          // Dynamically cache styling and assets if successful
          if (response.status === 200 && (
              response.url.endsWith('.js') || 
              response.url.endsWith('.css') || 
              response.url.includes('/assets/'))) {
            const responseClone = response.clone();
            caches.open(CACHE_NAME).then((cache) => {
              cache.put(event.request, responseClone);
            });
          }
          return response;
        });
      })
    );
  }
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
