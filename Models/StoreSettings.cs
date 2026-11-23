namespace HandmadeStore.Models;

public class StoreSettings
{
    public string Name { get; set; } = "لمسة يد";
    public string Tagline { get; set; } = "";
    public string Phone { get; set; } = "";
    public string WhatsApp { get; set; } = "";
    public string Instagram { get; set; } = "";
    public string Facebook { get; set; } = "";
    public string Address { get; set; } = "";

    /// <summary>مصاريف التوصيل بالجنيه</summary>
    public decimal DeliveryFee { get; set; } = 30;

    /// <summary>التوصيل مجاني لو إجمالي المنتجات وصل للرقم ده (0 = مفيش توصيل مجاني)</summary>
    public decimal FreeDeliveryOver { get; set; }

    /// <summary>السماح بالاستلام من المتجر</summary>
    public bool PickupEnabled { get; set; } = true;

    public decimal CalcDeliveryFee(decimal subtotal) =>
        FreeDeliveryOver > 0 && subtotal >= FreeDeliveryOver ? 0 : DeliveryFee;
}
