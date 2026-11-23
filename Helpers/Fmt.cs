using System.Globalization;
using HandmadeStore.Models;

namespace HandmadeStore.Helpers;

public static class Fmt
{
    private static readonly TimeZoneInfo Tz = FindTimeZone();

    private static TimeZoneInfo FindTimeZone()
    {
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }
        return TimeZoneInfo.Utc;
    }

    public static string Price(decimal price) =>
        price.ToString("#,0.##", CultureInfo.InvariantCulture) + " ج.م";

    public static string Date(DateTime utc)
    {
        var fixedKind = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(fixedKind, Tz).ToString("yyyy/MM/dd HH:mm", CultureInfo.InvariantCulture);
    }

    public static string StatusText(OrderStatus status) => status switch
    {
        OrderStatus.New => "جديد",
        OrderStatus.Confirmed => "تم التأكيد",
        OrderStatus.InProgress => "جاري التجهيز",
        OrderStatus.Shipped => "تم الشحن",
        OrderStatus.Delivered => "تم التسليم",
        OrderStatus.Cancelled => "ملغي",
        _ => "غير معروف"
    };

    public static string StatusBadge(OrderStatus status) => status switch
    {
        OrderStatus.New => "text-bg-warning",
        OrderStatus.Confirmed => "text-bg-info",
        OrderStatus.InProgress => "text-bg-primary",
        OrderStatus.Shipped => "text-bg-secondary",
        OrderStatus.Delivered => "text-bg-success",
        OrderStatus.Cancelled => "text-bg-danger",
        _ => "text-bg-light"
    };

    /// <summary>بيحوّل أي صيغة رقم مصري (+20 أو مسافات) لصيغة موحدة 01xxxxxxxxx</summary>
    public static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return "";
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0020"))
        {
            digits = digits.Substring(4);
        }
        else if (digits.StartsWith("20") && digits.Length == 12)
        {
            digits = digits.Substring(2);
        }

        if (digits.Length == 10 && digits.StartsWith("1"))
        {
            digits = "0" + digits;
        }

        return digits;
    }
}
