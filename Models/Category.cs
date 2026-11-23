using System.ComponentModel.DataAnnotations;

namespace HandmadeStore.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم التصنيف مطلوب")]
    [StringLength(80, ErrorMessage = "اسم التصنيف طويل جدًا")]
    [Display(Name = "اسم التصنيف")]
    public string Name { get; set; } = "";

    [StringLength(300, ErrorMessage = "الوصف طويل جدًا")]
    [Display(Name = "الوصف")]
    public string? Description { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
