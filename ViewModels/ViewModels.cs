using System.ComponentModel.DataAnnotations;
using HandmadeStore.Models;

namespace HandmadeStore.ViewModels;

public class CategoryCardViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public class HomeViewModel
{
    public List<Product> Featured { get; set; } = new();
    public List<Product> Latest { get; set; } = new();
    public List<CategoryCardViewModel> Categories { get; set; } = new();
}

public class ProductListViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public int? CategoryId { get; set; }
    public string? Q { get; set; }
    public string Sort { get; set; } = "new";
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = new();
    public List<Product> Related { get; set; } = new();
    public string? WhatsAppUrl { get; set; }
}

public class CartItemViewModel
{
    public Product Product { get; set; } = new();
    public int Quantity { get; set; }
    public decimal LineTotal => Product.Price * Quantity;
}

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.LineTotal);
    public int Count => Items.Sum(i => i.Quantity);
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "الاسم مطلوب")]
    [StringLength(100, ErrorMessage = "الاسم طويل جدًا")]
    [Display(Name = "الاسم بالكامل")]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "رقم الموبايل مطلوب")]
    [RegularExpression(@"^(\+?20|0)?1[0125][0-9]{8}$", ErrorMessage = "اكتب رقم موبايل مصري صحيح")]
    [Display(Name = "رقم الموبايل")]
    public string Phone { get; set; } = "";

    // العنوان مطلوب بس لو الاختيار توصيل (بيتفحص في الـ Controller)
    [StringLength(300, ErrorMessage = "العنوان طويل جدًا")]
    [Display(Name = "العنوان بالتفصيل")]
    public string Address { get; set; } = "";

    [Display(Name = "طريقة الاستلام")]
    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Delivery;

    [StringLength(500, ErrorMessage = "الملاحظات طويلة جدًا")]
    [Display(Name = "ملاحظات (اختياري)")]
    public string? Notes { get; set; }

    public CartViewModel? Cart { get; set; }

    // بتتملى من الإعدادات في الـ Controller
    public decimal DeliveryFee { get; set; }
    public decimal FreeDeliveryOver { get; set; }
    public bool PickupEnabled { get; set; }
    public string StoreAddress { get; set; } = "";
}

public class CustomerOrdersViewModel
{
    public ApplicationUser Customer { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public int OrdersCount { get; set; }
    public int ActiveCount { get; set; }
    public decimal TotalSpent { get; set; }
    public OrderStatus? Filter { get; set; }
}

public class TrackViewModel
{
    [Required(ErrorMessage = "كود الطلب مطلوب")]
    [StringLength(20)]
    [Display(Name = "كود الطلب")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "رقم الموبايل مطلوب")]
    [StringLength(20)]
    [Display(Name = "رقم الموبايل")]
    public string Phone { get; set; } = "";

    public Order? Result { get; set; }
    public bool Searched { get; set; }
}

public class LoginViewModel
{
    [Required(ErrorMessage = "اكتب البريد الإلكتروني أو رقم الموبايل")]
    [StringLength(150)]
    [Display(Name = "البريد الإلكتروني أو رقم الموبايل")]
    public string Identifier { get; set; } = "";

    [Required(ErrorMessage = "كلمة السر مطلوبة")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة السر")]
    public string Password { get; set; } = "";

    [Display(Name = "فضّلني مسجّل دخول")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "الاسم مطلوب")]
    [StringLength(100, ErrorMessage = "الاسم طويل جدًا")]
    [Display(Name = "الاسم بالكامل")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "رقم الموبايل مطلوب")]
    [RegularExpression(@"^(\+?20|0)?1[0125][0-9]{8}$", ErrorMessage = "اكتب رقم موبايل مصري صحيح")]
    [Display(Name = "رقم الموبايل")]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [StringLength(150, ErrorMessage = "البريد الإلكتروني طويل جدًا")]
    [Display(Name = "البريد الإلكتروني (اختياري)")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "كلمة السر مطلوبة")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة السر لازم تكون 8 حروف على الأقل")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة السر")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "اكتب كلمة السر تاني للتأكيد")]
    [Compare(nameof(Password), ErrorMessage = "كلمتا السر غير متطابقتين")]
    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة السر")]
    public string ConfirmPassword { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "الاسم مطلوب")]
    [StringLength(100, ErrorMessage = "الاسم طويل جدًا")]
    [Display(Name = "الاسم بالكامل")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "رقم الموبايل مطلوب")]
    [RegularExpression(@"^(\+?20|0)?1[0125][0-9]{8}$", ErrorMessage = "اكتب رقم موبايل مصري صحيح")]
    [Display(Name = "رقم الموبايل")]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [StringLength(150, ErrorMessage = "البريد الإلكتروني طويل جدًا")]
    [Display(Name = "البريد الإلكتروني (اختياري)")]
    public string? Email { get; set; }

    [StringLength(300, ErrorMessage = "العنوان طويل جدًا")]
    [Display(Name = "العنوان (اختياري، بيتكتب لوحده في الطلبات)")]
    public string? Address { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "اكتب كلمة السر الحالية")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة السر الحالية")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "اكتب كلمة السر الجديدة")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "كلمة السر لازم تكون 8 حروف على الأقل")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة السر الجديدة")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "اكتب كلمة السر الجديدة تاني للتأكيد")]
    [Compare(nameof(NewPassword), ErrorMessage = "كلمتا السر غير متطابقتين")]
    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة السر الجديدة")]
    public string ConfirmPassword { get; set; } = "";
}

public class AccountIndexViewModel
{
    public ApplicationUser Customer { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}

public class PagerViewModel
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public string Action { get; set; } = "Index";
    public Dictionary<string, string> Route { get; set; } = new();

    public Dictionary<string, string> WithPage(int page)
    {
        var copy = new Dictionary<string, string>(Route)
        {
            ["page"] = page.ToString()
        };
        return copy;
    }
}

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم المنتج مطلوب")]
    [StringLength(120, ErrorMessage = "الاسم طويل جدًا")]
    [Display(Name = "اسم المنتج")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "الوصف مطلوب")]
    [StringLength(2000, ErrorMessage = "الوصف طويل جدًا")]
    [Display(Name = "الوصف")]
    public string Description { get; set; } = "";

    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "السعر لازم يكون بين 1 و 1,000,000")]
    [Display(Name = "السعر (ج.م)")]
    public decimal Price { get; set; }

    [Display(Name = "التصنيف")]
    public int CategoryId { get; set; }

    [Range(0, 90, ErrorMessage = "مدة التجهيز بين 0 و 90 يوم")]
    [Display(Name = "مدة التجهيز (بالأيام)")]
    public int PrepDays { get; set; }

    [Display(Name = "متاح للطلب")]
    public bool IsAvailable { get; set; } = true;

    [Display(Name = "منتج مميز (يظهر في الرئيسية)")]
    public bool IsFeatured { get; set; }

    [Display(Name = "صورة المنتج")]
    public IFormFile? Image { get; set; }

    public string? ExistingImage { get; set; }

    [Display(Name = "حذف الصورة الحالية")]
    public bool RemoveImage { get; set; }
}
