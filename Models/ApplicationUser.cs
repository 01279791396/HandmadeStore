using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace HandmadeStore.Models;

/// <summary>
/// مستخدم الموقع (عميل أو أدمن) عن طريق ASP.NET Core Identity.
/// UserName = رقم الموبايل للعملاء، وإيميل الأدمن للأدمن.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>رقم الموبايل (نفس PhoneNumber بس بقيمة مش فاضية)</summary>
    [NotMapped]
    public string Phone => PhoneNumber ?? "";

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}
