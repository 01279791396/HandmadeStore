using HandmadeStore.Data;
using HandmadeStore.Helpers;
using HandmadeStore.Models;
using HandmadeStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ---------------------------------------------------------------- دخول

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectAfterLogin(returnUrl, User.IsInRole("Admin"));
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm.Password = "";
            return View(vm);
        }

        var identifier = vm.Identifier.Trim();

        // بنلاقي المستخدم بالإيميل أو رقم الموبايل (أو اسم المستخدم للأدمن)
        ApplicationUser? user;
        if (identifier.Contains('@'))
        {
            user = await _userManager.FindByEmailAsync(identifier);
        }
        else
        {
            user = await _userManager.FindByNameAsync(Fmt.NormalizePhone(identifier))
                   ?? await _userManager.FindByNameAsync(identifier);
        }

        if (user != null)
        {
            var result = await _signInManager.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, Roles.Admin);
                return RedirectAfterLogin(vm.ReturnUrl, isAdmin);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "اتقفل الحساب مؤقتًا بسبب محاولات غلط كتير. جرّب تاني بعد 10 دقايق");
                vm.Password = "";
                return View(vm);
            }
        }

        ModelState.AddModelError("", "البيانات اللي كتبتها غير صحيحة");
        vm.Password = "";
        return View(vm);
    }

    // ---------------------------------------------------------------- حساب جديد

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectAfterLogin(returnUrl, User.IsInRole("Admin"));
        }

        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        var phone = Fmt.NormalizePhone(vm.Phone);
        var email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim().ToLowerInvariant();

        if (ModelState.IsValid)
        {
            if (await _userManager.FindByNameAsync(phone) != null)
            {
                ModelState.AddModelError(nameof(vm.Phone), "رقم الموبايل ده مسجّل قبل كده، جرّب تسجّل دخول");
            }

            if (email != null && await _userManager.FindByEmailAsync(email) != null)
            {
                ModelState.AddModelError(nameof(vm.Email), "البريد الإلكتروني ده مسجّل قبل كده");
            }
        }

        if (!ModelState.IsValid)
        {
            vm.Password = "";
            vm.ConfirmPassword = "";
            return View(vm);
        }

        var user = new ApplicationUser
        {
            UserName = phone,
            PhoneNumber = phone,
            Email = email,
            Name = vm.Name.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        var created = await _userManager.CreateAsync(user, vm.Password);
        if (!created.Succeeded)
        {
            foreach (var error in created.Errors)
            {
                ModelState.AddModelError("", TranslateIdentityError(error));
            }

            vm.Password = "";
            vm.ConfirmPassword = "";
            return View(vm);
        }

        await _userManager.AddToRoleAsync(user, Roles.Customer);
        await _signInManager.SignInAsync(user, isPersistent: false);

        TempData["Success"] = "أهلًا بيك! تم إنشاء حسابك بنجاح";
        return RedirectAfterLogin(vm.ReturnUrl, false);
    }

    // ---------------------------------------------------------------- خروج

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ---------------------------------------------------------------- صفحة حسابي

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Index()
    {
        var customer = await CurrentCustomerAsync();
        if (customer == null)
        {
            return await ForceLogoutAsync();
        }

        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(new AccountIndexViewModel { Customer = customer, Orders = orders });
    }

    // ---------------------------------------------------------------- صفحة طلباتي (كل الطلبات + إحصائيات)

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Orders(OrderStatus? status)
    {
        var customer = await CurrentCustomerAsync();
        if (customer == null)
        {
            return await ForceLogoutAsync();
        }

        var all = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var vm = new CustomerOrdersViewModel
        {
            Customer = customer,
            OrdersCount = all.Count,
            ActiveCount = all.Count(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled),
            TotalSpent = all.Where(o => o.Status != OrderStatus.Cancelled).Sum(o => o.Total),
            Filter = status,
            Orders = status.HasValue ? all.Where(o => o.Status == status.Value).ToList() : all
        };

        return View(vm);
    }

    // ---------------------------------------------------------------- الفاتورة (بعد ما الأدمن يصدرها)

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Invoice(int id)
    {
        var customerId = CurrentUserId();
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);

        if (order == null || !order.HasInvoice)
        {
            return NotFound();
        }

        return View("~/Views/Shared/Invoice.cshtml", order);
    }

    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var customerId = CurrentUserId();
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Profile()
    {
        var customer = await CurrentCustomerAsync();
        if (customer == null)
        {
            return await ForceLogoutAsync();
        }

        return View(new ProfileViewModel
        {
            Name = customer.Name,
            Phone = customer.Phone,
            Email = customer.Email,
            Address = customer.Address
        });
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Profile(ProfileViewModel vm)
    {
        var customer = await CurrentCustomerAsync();
        if (customer == null)
        {
            return await ForceLogoutAsync();
        }

        var phone = Fmt.NormalizePhone(vm.Phone);
        var email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim().ToLowerInvariant();

        if (ModelState.IsValid)
        {
            var samePhone = await _userManager.FindByNameAsync(phone);
            if (samePhone != null && samePhone.Id != customer.Id)
            {
                ModelState.AddModelError(nameof(vm.Phone), "رقم الموبايل ده مستخدم في حساب تاني");
            }

            if (email != null)
            {
                var sameEmail = await _userManager.FindByEmailAsync(email);
                if (sameEmail != null && sameEmail.Id != customer.Id)
                {
                    ModelState.AddModelError(nameof(vm.Email), "البريد الإلكتروني ده مستخدم في حساب تاني");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        customer.Name = vm.Name.Trim();
        customer.UserName = phone;
        customer.PhoneNumber = phone;
        customer.Email = email;
        customer.Address = string.IsNullOrWhiteSpace(vm.Address) ? null : vm.Address.Trim();

        var updated = await _userManager.UpdateAsync(customer);
        if (!updated.Succeeded)
        {
            foreach (var error in updated.Errors)
            {
                ModelState.AddModelError("", TranslateIdentityError(error));
            }

            return View(vm);
        }

        // نجدد الكوكي عشان الاسم الجديد يظهر
        await _signInManager.RefreshSignInAsync(customer);

        TempData["Success"] = "تم حفظ بياناتك";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        var customer = await CurrentCustomerAsync();
        if (customer == null)
        {
            return await ForceLogoutAsync();
        }

        if (!ModelState.IsValid)
        {
            return View(new ChangePasswordViewModel());
        }

        var result = await _userManager.ChangePasswordAsync(customer, vm.CurrentPassword, vm.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    error.Code == nameof(IdentityErrorDescriber.PasswordMismatch) ? nameof(vm.CurrentPassword) : "",
                    TranslateIdentityError(error));
            }

            return View(new ChangePasswordViewModel());
        }

        await _signInManager.RefreshSignInAsync(customer);

        TempData["Success"] = "تم تغيير كلمة السر";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------- أدوات مساعدة

    private string? CurrentUserId() => _userManager.GetUserId(User);

    private async Task<ApplicationUser?> CurrentCustomerAsync()
    {
        var id = CurrentUserId();
        return id == null ? null : await _userManager.FindByIdAsync(id);
    }

    private async Task<IActionResult> ForceLogoutAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    private static string TranslateIdentityError(IdentityError error) => error.Code switch
    {
        nameof(IdentityErrorDescriber.PasswordMismatch) => "كلمة السر الحالية غير صحيحة",
        nameof(IdentityErrorDescriber.PasswordTooShort) => "كلمة السر لازم تكون 8 حروف على الأقل",
        nameof(IdentityErrorDescriber.DuplicateUserName) => "رقم الموبايل ده مسجّل قبل كده",
        nameof(IdentityErrorDescriber.DuplicateEmail) => "البريد الإلكتروني ده مسجّل قبل كده",
        nameof(IdentityErrorDescriber.InvalidUserName) => "رقم الموبايل غير صحيح",
        nameof(IdentityErrorDescriber.InvalidEmail) => "البريد الإلكتروني غير صحيح",
        _ => "حصل خطأ، راجع البيانات وجرّب تاني"
    };

    private IActionResult RedirectAfterLogin(string? returnUrl, bool isAdmin)
    {
        var allowed = !string.IsNullOrEmpty(returnUrl)
                      && returnUrl != "/"
                      && Url.IsLocalUrl(returnUrl)
                      && (isAdmin || !returnUrl.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase));

        if (allowed)
        {
            return LocalRedirect(returnUrl!);
        }

        return isAdmin
            ? RedirectToAction("Index", "Dashboard", new { area = "Admin" })
            : RedirectToAction("Index", "Home");
    }
}
