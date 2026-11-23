using HandmadeStore.Data;
using HandmadeStore.Models;
using HandmadeStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HandmadeStore.Controllers;

public class ProductsController : Controller
{
    private const int PageSize = 9;

    private readonly AppDbContext _db;
    private readonly StoreSettings _store;

    public ProductsController(AppDbContext db, IOptions<StoreSettings> store)
    {
        _db = db;
        _store = store.Value;
    }

    public async Task<IActionResult> Index(int? categoryId, string? q, string sort = "new", int page = 1)
    {
        var query = _db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term) || p.Description.Contains(term));
        }

        query = sort switch
        {
            "price_asc" => query.OrderByDescending(p => p.IsAvailable).ThenBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.IsAvailable).ThenByDescending(p => p.Price),
            _ => query.OrderByDescending(p => p.IsAvailable).ThenByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var vm = new ProductListViewModel
        {
            Products = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(),
            Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
            CategoryId = categoryId,
            Q = q,
            Sort = sort,
            Page = page,
            TotalPages = totalPages,
            TotalCount = total
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        var related = await _db.Products.AsNoTracking()
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsAvailable)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToListAsync();

        string? whatsapp = null;
        var number = new string((_store.WhatsApp ?? "").Where(char.IsDigit).ToArray());
        if (number.Length > 0)
        {
            var link = Url.Action("Details", "Products", new { id = product.Id }, Request.Scheme);
            var text = $"مرحبًا، عايز أطلب: {product.Name}\n{link}";
            whatsapp = $"https://wa.me/{number}?text={Uri.EscapeDataString(text)}";
        }

        return View(new ProductDetailsViewModel
        {
            Product = product,
            Related = related,
            WhatsAppUrl = whatsapp
        });
    }
}
