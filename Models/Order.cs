using System.ComponentModel.DataAnnotations.Schema;

namespace HandmadeStore.Models;

public enum OrderStatus
{
    New = 0,
    Confirmed = 1,
    InProgress = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public enum DeliveryMethod
{
    /// <summary>توصيل لحد العنوان (بمصاريف توصيل)</summary>
    Delivery = 0,
    /// <summary>استلام من المتجر (من غير مصاريف)</summary>
    Pickup = 1
}

public class Order
{
    public int Id { get; set; }

    /// <summary>كود الطلب العشوائي اللي بيظهر للعميل</summary>
    public string Code { get; set; } = "";

    public string CustomerName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Address { get; set; } = "";
    public string? Notes { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>الإجمالي النهائي = إجمالي المنتجات + مصاريف التوصيل</summary>
    public decimal Total { get; set; }

    public DeliveryMethod DeliveryMethod { get; set; } = DeliveryMethod.Delivery;

    /// <summary>مصاريف التوصيل وقت الطلب (0 لو استلام من المتجر أو توصيل مجاني)</summary>
    public decimal DeliveryFee { get; set; }

    /// <summary>رقم الفاتورة (فاضي لحد ما الأدمن يصدرها)</summary>
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceIssuedAt { get; set; }

    [NotMapped]
    public decimal Subtotal => Items.Sum(i => i.UnitPrice * i.Quantity);

    [NotMapped]
    public bool HasInvoice => !string.IsNullOrEmpty(InvoiceNumber);

    /// <summary>الحساب اللي عمل الطلب (فاضي لو الطلب كان كزائر)</summary>
    public string? CustomerId { get; set; }
    public ApplicationUser? Customer { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>نسخة من اسم وسعر المنتج وقت الطلب</summary>
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    [NotMapped]
    public decimal LineTotal => UnitPrice * Quantity;
}
