(function () {
    'use strict';

    // ---------- تسجيل الـ Service Worker ----------
    if ('serviceWorker' in navigator) {
        window.addEventListener('load', function () {
            navigator.serviceWorker.register('/sw.js', { scope: '/' }).catch(function () { /* تجاهل */ });
        });
    }

    // ---------- تثبيت التطبيق ----------
    var banner = document.getElementById('installBanner');
    var installButtons = document.querySelectorAll('[data-install]');
    var closeBtn = document.getElementById('installClose');
    var iosHint = document.getElementById('installIosHint');
    var textEl = document.getElementById('installText');
    var deferredPrompt = null;

    function safeGet(k) { try { return localStorage.getItem(k); } catch (e) { return null; } }
    function safeSet(k, v) { try { localStorage.setItem(k, v); } catch (e) { /* تجاهل */ } }

    var isStandalone = window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    if (isStandalone) { return; }

    var ua = window.navigator.userAgent || '';
    var isIos = /iphone|ipad|ipod/i.test(ua) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    var dismissedAt = parseInt(safeGet('hm-install-dismissed') || '0', 10);
    var recentlyDismissed = dismissedAt && (Date.now() - dismissedAt) < 7 * 24 * 60 * 60 * 1000;

    function showBanner() {
        if (banner && !recentlyDismissed) { banner.hidden = false; }
    }

    function showInstallButtons() {
        installButtons.forEach(function (b) { b.hidden = false; });
    }

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;
        showInstallButtons();
        showBanner();
    });

    window.addEventListener('appinstalled', function () {
        deferredPrompt = null;
        if (banner) { banner.hidden = true; }
        installButtons.forEach(function (b) { b.hidden = true; });
    });

    installButtons.forEach(function (btn) {
        btn.addEventListener('click', function () {
            if (deferredPrompt) {
                deferredPrompt.prompt();
                deferredPrompt.userChoice.finally(function () {
                    deferredPrompt = null;
                    if (banner) { banner.hidden = true; }
                });
            } else if (isIos && iosHint) {
                iosHint.hidden = !iosHint.hidden;
            }
        });
    });

    if (closeBtn) {
        closeBtn.addEventListener('click', function () {
            safeSet('hm-install-dismissed', String(Date.now()));
            if (banner) { banner.hidden = true; }
        });
    }

    // آيفون (Safari) مفيهوش زرار تثبيت تلقائي: بنعرض تعليمات
    if (isIos) {
        showInstallButtons();
        if (textEl) { textEl.textContent = 'ثبّت التطبيق على موبايلك'; }
        if (iosHint) { iosHint.hidden = false; }
        showBanner();
    }
})();
