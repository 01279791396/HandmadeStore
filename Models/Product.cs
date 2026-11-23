using System.ComponentModel.DataAnnotations.Schema;

namespace HandmadeStore.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string? ImageFileName { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; }

    /// <summary>عدد أيام تجهيز القطعة (0 = جاهزة للتسليم)</summary>
    public int PrepDays { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public string ImageUrl => string.IsNullOrEmpty(ImageFileName)
        ? "/img/placeholder.svg"
        : "/uploads/" + ImageFileName;
}
