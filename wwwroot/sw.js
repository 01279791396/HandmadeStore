/* Service Worker لموقع لمسة يد
 * - الصفحات (HTML) بتيجي من النت دايمًا (مش بنخزّنها عشان بيانات الحساب والسلة خاصة)
 *   ولو مفيش نت بنعرض صفحة offline.html.
 * - الملفات الثابتة (CSS/JS/صور/خطوط) بنقدمها من الكاش وبنحدّثها في الخلفية.
 * غيّر رقم VERSION لما تعدّل الملف ده عشان الكاش القديم يتمسح. */
const VERSION = 'hm-v1';
const CACHE = 'static-' + VERSION;
const OFFLINE_URL = '/offline.html';
const PRECACHE = [
  OFFLINE_URL,
  '/img/favicon.svg',
  '/img/placeholder.svg',
  '/icons/icon-192.png',
  '/icons/icon-512.png'
];

const STATIC_PATHS = ['/css/', '/js/', '/img/', '/icons/', '/uploads/'];
const CDN_HOSTS = ['cdn.jsdelivr.net', 'fonts.googleapis.com', 'fonts.gstatic.com'];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE)
      .then((cache) => cache.addAll(PRECACHE))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(keys.filter((k) => k !== CACHE).map((k) => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

function isStaticRequest(url) {
  if (url.origin === self.location.origin) {
    return STATIC_PATHS.some((p) => url.pathname.startsWith(p));
  }
  return CDN_HOSTS.includes(url.hostname);
}

self.addEventListener('fetch', (event) => {
  const request = event.request;
  if (request.method !== 'GET') return;

  const url = new URL(request.url);

  // تنقّل بين الصفحات: من النت، ولو فشل نعرض صفحة "مفيش إنترنت"
  if (request.mode === 'navigate') {
    event.respondWith(
      fetch(request).catch(() => caches.match(OFFLINE_URL))
    );
    return;
  }

  // ملفات ثابتة: من الكاش فورًا وتتحدّث في الخلفية
  if (isStaticRequest(url)) {
    event.respondWith(
      caches.open(CACHE).then((cache) =>
        cache.match(request).then((cached) => {
          const network = fetch(request)
            .then((response) => {
              if (response && (response.ok || response.type === 'opaque')) {
                cache.put(request, response.clone());
              }
              return response;
            })
            .catch(() => cached);
          return cached || network;
        })
      )
    );
  }
});
