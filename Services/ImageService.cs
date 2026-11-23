namespace HandmadeStore.Services;

public class ImageService
{
    private const long MaxBytes = 20 * 1024 * 1024;
    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".jfif", ".png", ".webp", ".gif", ".bmp", ".avif" };

    private readonly IWebHostEnvironment _env;

    public ImageService(IWebHostEnvironment env)
    {
        _env = env;
    }

    private string UploadsDir => Path.Combine(_env.WebRootPath, "uploads");

    public async Task<(string? FileName, string? Error)> SaveAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            return (null, "ملف الصورة فاضي");
        }

        if (file.Length > MaxBytes)
        {
            return (null, "حجم الصورة لازم يكون أقل من 20 ميجا");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension is ".heic" or ".heif")
        {
            return (null, "صور الآيفون بصيغة HEIC مش بتتعرض في المتصفحات. حوّلها JPG الأول (أو صوّر بصيغة Most Compatible).");
        }

        if (!AllowedExtensions.Contains(extension))
        {
            return (null, "الصيغ المسموحة: JPG, PNG, WEBP, GIF, BMP, AVIF");
        }

        var header = new byte[16];
        int read;
        await using (var stream = file.OpenReadStream())
        {
            read = await stream.ReadAsync(header, 0, header.Length);
        }

        if (read < 12 || !LooksLikeImage(header))
        {
            return (null, "الملف مش صورة صالحة");
        }

        Directory.CreateDirectory(UploadsDir);
        var fileName = Guid.NewGuid().ToString("N") + extension;
        var fullPath = Path.Combine(UploadsDir, fileName);

        await using (var output = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(output);
        }

        return (fileName, null);
    }

    public void Delete(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var safeName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(UploadsDir, safeName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    // بنتأكد من محتوى الملف الحقيقي (مش الامتداد بس)
    private static bool LooksLikeImage(byte[] h)
    {
        var jpeg = h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF;
        var png = h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47;
        var gif = h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38;
        var bmp = h[0] == 0x42 && h[1] == 0x4D;
        var webp = h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                   && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50;
        var avif = h[4] == 0x66 && h[5] == 0x74 && h[6] == 0x79 && h[7] == 0x70
                   && h[8] == 0x61 && h[9] == 0x76 && h[10] == 0x69;
        return jpeg || png || gif || bmp || webp || avif;
    }
}
