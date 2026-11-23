using System.Security.Cryptography;

namespace HandmadeStore.Helpers;

public static class OrderCode
{
    // من غير الحروف والأرقام المتشابهة (0/O و1/I) عشان العميل يقدر يكتبه بسهولة
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Generate()
    {
        var chars = new char[6];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return "HM-" + new string(chars);
    }

    /// <summary>بيقبل الكود بأي شكل: صغير/كبير، بمسافات أو من غير شرطة</summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        var s = new string(input.Where(char.IsAsciiLetterOrDigit).ToArray()).ToUpperInvariant();

        if (s.StartsWith("HM") && s.Length == 8)
        {
            return "HM-" + s.Substring(2);
        }

        if (s.Length == 6)
        {
            return "HM-" + s;
        }

        return s;
    }
}
