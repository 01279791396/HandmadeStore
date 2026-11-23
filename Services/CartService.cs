using System.Text.Json;
using HandmadeStore.Data;
using HandmadeStore.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Services;

public class CartLine
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CartService
{
    public const int MaxQuantity = 20;
    private const string SessionKey = "cart";

    private readonly IHttpContextAccessor _http;
    private readonly AppDbContext _db;

    public CartService(IHttpContextAccessor http, AppDbContext db)
    {
        _http = http;
        _db = db;
    }

    private ISession Session => _http.HttpContext!.Session;

    public List<CartLine> GetLines()
    {
        var json = Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json))
        {
            return new List<CartLine>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<CartLine>>(json) ?? new List<CartLine>();
        }
        catch (JsonException)
        {
            return new List<CartLine>();
        }
    }

    private void Save(List<CartLine> lines) =>
        Session.SetString(SessionKey, JsonSerializer.Serialize(lines));

    public int Count() => _http.HttpContext?.Session == null ? 0 : GetLines().Sum(l => l.Quantity);

    public void Add(int productId, int quantity)
    {
        var lines = GetLines();
        var line = lines.FirstOrDefault(l => l.ProductId == productId);
        if (line == null)
        {
            lines.Add(new CartLine { ProductId = productId, Quantity = Math.Clamp(quantity, 1, MaxQuantity) });
        }
        else
        {
            line.Quantity = Math.Clamp(line.Quantity + quantity, 1, MaxQuantity);
        }
        Save(lines);
    }

    public void SetQuantity(int productId, int quantity)
    {
        var lines = GetLines();
        var line = lines.FirstOrDefault(l => l.ProductId == productId);
        if (line == null)
        {
            return;
        }

        if (quantity <= 0)
        {
            lines.Remove(line);
        }
        else
        {
            line.Quantity = Math.Clamp(quantity, 1, MaxQuantity);
        }
        Save(lines);
    }

    public void Remove(int productId)
    {
        var lines = GetLines();
        lines.RemoveAll(l => l.ProductId == productId);
        Save(lines);
    }

    public void Clear() => Session.Remove(SessionKey);

    /// <summary>بيبني السلة من الداتا بيز ويشيل أي منتج اتحذف</summary>
    public async Task<CartViewModel> BuildAsync()
    {
        var lines = GetLines();
        var ids = lines.Select(l => l.ProductId).ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();

        var vm = new CartViewModel();
        var cleaned = new List<CartLine>();

        foreach (var line in lines)
        {
            var product = products.FirstOrDefault(p => p.Id == line.ProductId);
            if (product == null)
            {
                continue;
            }

            cleaned.Add(line);
            vm.Items.Add(new CartItemViewModel { Product = product, Quantity = line.Quantity });
        }

        if (cleaned.Count != lines.Count)
        {
            Save(cleaned);
        }

        return vm;
    }
}
