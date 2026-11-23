# لمسة يد: موقع عرض وبيع منتجات الهاند ميد (ASP.NET Core 8 MVC)

موقع بيعرض منتجات صنع يدوي. العميل لازم يعمل حساب ويدخل، يتصفح المنتجات ويطلب بالدفع عند الاستلام (توصيل أو استلام من المتجر)، ويتابع طلباته ويشوف فواتيره. صاحب المتجر بيدير كل حاجة من لوحة تحكم. الموقع كمان **PWA**: بيتثبّت على الموبايل كأنه تطبيق.

## المتطلبات
- .NET 8 SDK
- SQL Server (LocalDB جاي مع Visual Studio)

## التشغيل على جهازك (Visual Studio)
1. افتح المشروع.
2. `appsettings.json` ← `ConnectionStrings:Default` (الافتراضي LocalDB: `Server=(localdb)\\MSSQLLocalDB;Database=HandmadeStore;Trusted_Connection=True;TrustServerCertificate=True`).
3. من **Package Manager Console**:
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
4. شغّل (F5). التطبيق بيطبّق الـ Migrations تلقائيًا، وبيعمل الأدوار وحساب الأدمن (من قسم `Admin`) وتصنيفات ومنتجات تجريبية أول مرة.
5. بعد أي تعديل في الـ Models: `Add-Migration اسم_التعديل` ثم `Update-Database`.

> مهم: لازم يكون فيه مجلد `Migrations` (وارفعه على GitHub). من غيره التطبيق بيوقف ويعرض رسالة بتقول كده.
> لو عايز تبدأ من الصفر: امسح مجلد `Migrations` وقاعدة `HandmadeStore` المحلية، وكرر الخطوة 3.

## الحسابات (ASP.NET Core Identity)
- العميل: اسم المستخدم = رقم موبايله، وبيدخل بالموبايل أو بالإيميل (اختياري). الأدمن بيدخل بالإيميل/اسم المستخدم من `Admin` في الإعدادات.
- الأدوار: `Admin` و`Customer`. كلمة السر 8 حروف على الأقل، والحساب بيتقفل 10 دقايق بعد 5 محاولات غلط.
- الأدمن بيتعمل **مرة واحدة** بس (لو مفيش أي أدمن في الداتابيز)، فاضبط `Admin:UserName` و`Admin:Password` قبل أول تشغيل.
- الموقع كله محتاج تسجيل دخول، ما عدا صفحات الدخول والتسجيل.

## إعدادات المتجر (`appsettings.json` ← `Store`)
الاسم، الشعار، الموبايل، الواتساب (بصيغة دولية بدون +، مثلًا `201012345678`)، انستجرام، فيسبوك، العنوان، وكمان:
- `DeliveryFee`: مصاريف التوصيل.
- `FreeDeliveryOver`: توصيل مجاني فوق مبلغ معين (0 = مقفول).
- `PickupEnabled`: السماح بالاستلام من المتجر.

## التوصيل والفواتير
- العميل بيختار توصيل أو استلام من المتجر، والمصاريف بتتسجل في الطلب وقت الشراء.
- العميل بيشوف طلباته وإحصائياته من **طلباتي** (`/Account/Orders`).
- الأدمن من تفاصيل الطلب يضغط **إصدار فاتورة**؛ بعدها بتظهر للعميل في حسابه، وتتطبع أو تتحفظ PDF من زرار الطباعة (اختار Save as PDF).

## تثبيت التطبيق على الموبايل (PWA)
- أندرويد (Chrome/Brave): بيظهر شريط "ثبّت التطبيق" وزرار في الفوتر. أو من قائمة المتصفح: **Install app / تثبيت التطبيق**.
- آيفون (Safari): زرار المشاركة ← **إضافة إلى الشاشة الرئيسية**.
- لازم الموقع يشتغل على **HTTPS** (MonsterASP بيوفره).
- الملفات: `wwwroot/manifest.webmanifest`، `wwwroot/sw.js`، `wwwroot/offline.html`، `wwwroot/js/pwa.js`، والأيقونات في `wwwroot/icons`.
- الـ Service Worker بيخزّن الملفات الثابتة بس (CSS/JS/صور). صفحات الحساب والسلة **مش** بتتخزّن. لو مفيش إنترنت بتظهر صفحة "مفيش اتصال".
- لو عدّلت `sw.js` غيّر `VERSION` جواه.

## النشر على MonsterASP (GitHub + Git deploy)
1. **GitHub:** ارفع المشروع (Repository **Private**). `.gitignore` جاهز ومانع رفع `bin/obj` وأسرار السيرفر والصور المرفوعة.
2. **MonsterASP:** اعمل Website وقاعدة **MSSQL**، وانسخ الـ **Local** connection string.
3. **متغيرات البيئة** (الموقع ← Scripting ← Environment variables) وبعدها **Restart application**:

   | الاسم | القيمة |
   |---|---|
   | `ConnectionStrings__Default` | الـ Local connection string |
   | `Admin__UserName` | إيميل الأدمن |
   | `Admin__Password` | كلمة سر الأدمن (قبل أول تشغيل) |
   | `Store__Phone`, `Store__WhatsApp` ... | اختياري، بدل `appsettings.json` |
   | `Diagnostics__ShowStartupError` | `true` مؤقتًا لو فيه مشكلة تشغيل، وبعدها `false` |

4. **Git deploy:** الموقع ← Git deploy ← رابط الـ Repository (ينتهي بـ `.git`) + token لو Private ← Load ← Branch ← النوع **Build .NET app** ← Enable.
5. **Pull latest** ثم **Build & publish** ثم **Restart application**.
6. الشغل اليومي: عدّل ← Commit وPush من Visual Studio ← **Pull latest** و**Build & publish** على MonsterASP.

ملاحظات:
- الـ Migrations بتتطبق لوحدها وقت التشغيل على قاعدة MonsterASP؛ ما تحتاجش `Update-Database` عليها. لازم قاعدة MonsterASP تكون فاضية أو من نفس الـ Migrations.
- مفاتيح حماية الكوكيز بتتحفظ في `App_Data/keys` عشان ما تتبوّظش مع إعادة تشغيل التطبيق، والنشر مش بيمسح `App_Data` ولا `wwwroot/uploads`.
- صور المنتجات ارفعها من لوحة الأدمن على الموقع الفعلي (مش من جهازك).

## لو فيه مشكلة
- **500.30:** التطبيق وقع وقت التشغيل. فعّل `Diagnostics__ShowStartupError=true` وافتح الموقع، هتشوف سبب الخطأ (أغلبه Connection String أو Migrations ناقصة).
- **404 من IIS (صفحة Server Error):** معناها مفيش تطبيق منشور، يعني الـ Build فشل. افتح **Recent builds** على MonsterASP وشوف أسطر `error CS`.
- **"already an object named ... in the database":** الداتابيز فيها جداول قديمة، فضّيها (احذف الجداول من WebMSSQL أو اعمل قاعدة جديدة) وعيد التشغيل.

## هيكل المشروع
- `Controllers/` الواجهة (حساب، منتجات، سلة، طلب، تتبع)
- `Areas/Admin/` لوحة الأدمن (طلبات، منتجات، تصنيفات، عملاء، فواتير)
- `Models/`, `Data/` (AppDbContext + Seeder)، `Services/`، `Helpers/`، `ViewModels/`
- `Views/`, `wwwroot/` (css/js/img/icons + ملفات PWA)
