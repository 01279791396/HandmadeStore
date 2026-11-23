using HandmadeStore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { Roles.Admin, Roles.Customer })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // أول أدمن بيتعمل مرة واحدة بس (لو مفيش أي أدمن)
        if ((await userManager.GetUsersInRoleAsync(Roles.Admin)).Count == 0)
        {
            var userName = (config["Admin:UserName"] ?? "admin").Trim();
            var password = config["Admin:Password"] ?? "Admin@12345";

            var admin = new ApplicationUser
            {
                UserName = userName,
                Email = userName.Contains('@') ? userName : null,
                EmailConfirmed = true,
                Name = "مدير المتجر",
                CreatedAt = DateTime.UtcNow
            };

            var created = await userManager.CreateAsync(admin, password);
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.Admin);

                if (password == "Admin@12345")
                {
                    logger.LogWarning("حساب الأدمن بيستخدم كلمة السر الافتراضية. غيّرها في الإعدادات قبل النشر.");
                }
            }
            else
            {
                logger.LogError("فشل إنشاء حساب الأدمن: {Errors}", string.Join("; ", created.Errors.Select(e => e.Description)));
            }
        }

        if (await db.Categories.AnyAsync())
        {
            return;
        }

        var candles = new Category { Name = "شموع ومعطرات", Description = "شموع معطرة وصب يدوي" };
        var crochet = new Category { Name = "كروشيه وتريكو", Description = "قطع منسوجة بالإيد" };
        var accessories = new Category { Name = "إكسسوارات", Description = "خواتم وسلاسل وأساور" };
        var decor = new Category { Name = "ديكور وهدايا", Description = "قطع ديكور وهدايا مميزة" };
        db.Categories.AddRange(candles, crochet, accessories, decor);
        await db.SaveChangesAsync();

        var now = DateTime.UtcNow;
        db.Products.AddRange(
            new Product
            {
                Name = "شمعة لافندر معطرة",
                Description = "شمعة صويا طبيعية بريحة اللافندر الهادية، بتولع حوالي 30 ساعة. معمولة بالإيد في عبوة زجاج قابلة لإعادة الاستخدام.",
                Price = 120, CategoryId = candles.Id, IsFeatured = true, PrepDays = 2, CreatedAt = now.AddMinutes(-1)
            },
            new Product
            {
                Name = "طقم شموع هدية (3 قطع)",
                Description = "طقم من 3 شموع بروائح مختلفة (فانيليا، قرفة، ورد) في علبة هدية جاهزة.",
                Price = 320, CategoryId = candles.Id, IsFeatured = true, PrepDays = 3, CreatedAt = now.AddMinutes(-2)
            },
            new Product
            {
                Name = "مفرش كروشيه دائري",
                Description = "مفرش كروشيه بخيط قطن 100% بقطر 40 سم، مناسب للطاولات والترابيزات الصغيرة.",
                Price = 180, CategoryId = crochet.Id, IsFeatured = true, PrepDays = 5, CreatedAt = now.AddMinutes(-3)
            },
            new Product
            {
                Name = "شنطة كروشيه صيفي",
                Description = "شنطة كتف مصنوعة يدويًا من خيط الرافيا، خفيفة وعملية وتناسب الخروج والبحر.",
                Price = 450, CategoryId = crochet.Id, PrepDays = 7, CreatedAt = now.AddMinutes(-4)
            },
            new Product
            {
                Name = "سلسلة حجر طبيعي",
                Description = "سلسلة بخرز حجر طبيعي وإطار ستانلس ستيل ما بيصدّيش ولا بيغيّر لونه.",
                Price = 150, CategoryId = accessories.Id, IsFeatured = true, PrepDays = 2, CreatedAt = now.AddMinutes(-5)
            },
            new Product
            {
                Name = "أسورة خرز ملونة",
                Description = "أسورة مرنة بخرز ملون، بتتعمل بمقاس إيدك.",
                Price = 70, CategoryId = accessories.Id, PrepDays = 1, CreatedAt = now.AddMinutes(-6)
            },
            new Product
            {
                Name = "حامل مفاتيح خشب منقوش",
                Description = "حامل مفاتيح من خشب الزان بنقش يدوي، تقدر تطلب إضافة اسم أو كلمة.",
                Price = 95, CategoryId = decor.Id, PrepDays = 3, CreatedAt = now.AddMinutes(-7)
            },
            new Product
            {
                Name = "إطار صورة مزخرف",
                Description = "إطار صورة 20×25 سم بزخارف مرسومة بالإيد، هدية مميزة للمناسبات.",
                Price = 210, CategoryId = decor.Id, IsFeatured = true, PrepDays = 4, CreatedAt = now.AddMinutes(-8)
            }
        );
        await db.SaveChangesAsync();
    }
}
