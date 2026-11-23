using System.Reflection;
using System.Threading.RateLimiting;
using HandmadeStore.Data;
using HandmadeStore.Models;
using HandmadeStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null)));

// على الاستضافة (IIS) مفاتيح الحماية بتضيع مع إعادة تشغيل التطبيق وبتبوّظ الكوكيز والـ Antiforgery.
// فبنحفظها في App_Data/keys (مجلد مش بيتمسح مع النشر ومش متاح من المتصفح).
try
{
    var keysDir = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys"));
    keysDir.Create();
    builder.Services.AddDataProtection()
        .SetApplicationName("HandmadeStore")
        .PersistKeysToFileSystem(keysDir);
}
catch (Exception)
{
    // مفيش صلاحية كتابة: هنكمل بالمفاتيح المؤقتة
}

builder.Services.Configure<StoreSettings>(builder.Configuration.GetSection("Store"));
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.Name = ".Handmade.Cart";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ImageService>();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        // رقم الموبايل هو اسم المستخدم، فبنسمح بالأرقام
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

        // قفل الحساب مؤقتًا بعد محاولات دخول غلط
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<AppClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = ".Handmade.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    // أي صفحة لازم يكون المستخدم مسجّل دخول، إلا اللي عليها [AllowAnonymous]
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        "login:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.AddPolicy("lookup", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        "lookup:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

var app = builder.Build();

// لو تهيئة الداتابيز فشلت، نكتب الخطأ في ملف ونعرضه في الصفحة (مؤقتًا للتشخيص)
// بدل ما التطبيق يقع بـ 500.30 من غير أي تفاصيل.
string? startupError = null;

// أدوات EF (Add-Migration / Update-Database) بتشغّل البرنامج؛ ما نلمسش الداتابيز في الحالة دي
var isEfTool = Assembly.GetEntryAssembly()?.GetName().Name is "ef" or "dotnet-ef";

try
{
    if (!isEfTool)
    {
        var connectionString = builder.Configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Default' غير موجود أو فارغ في appsettings.json.");
        }

        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.Database.GetMigrations().Any())
        {
            throw new InvalidOperationException(
                "مفيش Migrations في المشروع. من Package Manager Console نفّذ Add-Migration InitialCreate " +
                "وارفع مجلد Migrations مع الكود.");
        }

        // بنطبّق الـ Migrations مرة واحدة فقط (الجداول بتتعمل من Add-Migration + Migrate)
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(scope.ServiceProvider, app.Configuration, app.Logger);
    }
}
catch (Exception ex)
{
    startupError = ex.ToString();
    app.Logger.LogError(ex, "Database initialization failed");
    try
    {
        File.WriteAllText(Path.Combine(app.Environment.ContentRootPath, "startup-error.txt"), startupError);
    }
    catch
    {
        // مفيش صلاحية كتابة، مش مشكلة
    }
}

if (startupError != null)
{
    // اقفل الـ Setting ده (Diagnostics:ShowStartupError=false) بعد ما المشكلة تتحل
    var show = app.Configuration.GetValue<bool>("Diagnostics:ShowStartupError");
    app.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync(show
            ? "Startup error:\n\n" + startupError
            : "الموقع تحت الصيانة، حاول بعد شوية.");
    });
    app.Run();
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // الـ Service Worker والـ manifest لازم يتجددوا على طول، مش يتخزّنوا
        if (ctx.File.Name is "sw.js" or "manifest.webmanifest" or "offline.html")
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache";
        }
    }
});
app.UseRouting();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();