// تأكيد قبل أي نموذج عليه data-confirm (زي حذف منتج)
document.addEventListener('submit', function (e) {
    var form = e.target;
    if (form && form.dataset && form.dataset.confirm) {
        if (!window.confirm(form.dataset.confirm)) {
            e.preventDefault();
        }
    }
});

// منع الضغط المزدوج على أزرار الإرسال
document.addEventListener('submit', function (e) {
    if (e.defaultPrevented) { return; }
    var btn = e.target.querySelector('button[type="submit"]');
    if (btn) {
        setTimeout(function () { btn.disabled = true; }, 0);
    }
});

// الرجوع من الكاش بعد الإرسال: نفعّل الأزرار تاني
window.addEventListener('pageshow', function (e) {
    if (e.persisted) {
        document.querySelectorAll('button[type="submit"]').forEach(function (b) { b.disabled = false; });
    }
});

// زرار نسخ النص (زي كود الطلب)
document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-copy]');
    if (!btn) { return; }
    var target = document.querySelector(btn.dataset.copy);
    if (!target) { return; }
    var label = btn.querySelector('span');
    var done = function () {
        if (label) {
            var old = label.textContent;
            label.textContent = 'تم النسخ';
            setTimeout(function () { label.textContent = old; }, 1500);
        }
    };
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(target.textContent.trim()).then(done);
    }
});

// إظهار/إخفاء كلمة السر
document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-toggle-password]');
    if (!btn) { return; }
    var input = document.querySelector(btn.dataset.togglePassword);
    if (!input) { return; }
    var show = input.type === 'password';
    input.type = show ? 'text' : 'password';
    var icon = btn.querySelector('i');
    if (icon) { icon.className = show ? 'bi bi-eye-slash' : 'bi bi-eye'; }
});
