using HandmadeStore.Data;
using HandmadeStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Controllers;

public class CartController : Controller
{
    private readonly AppDbContext _db;
    private readonly CartService _cart;

    public CartController(AppDbContext db, CartService cart)
    {
        _db = db;
        _cart = cart;
    }

    public async Task<IActionResult> Index() => View(await _cart.BuildAsync());

    [HttpPost]
    public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
        {
            return NotFound();
        }

        if (!product.IsAvailable)
        {
            TempData["Error"] = "المنتج ده غير متاح حاليًا";
            return Back(returnUrl);
        }

        _cart.Add(productId, Math.Clamp(quantity, 1, CartService.MaxQuantity));
        TempData["Success"] = $"تمت إضافة \"{product.Name}\" إلى السلة";
        return Back(returnUrl);
    }

    [HttpPost]
    public IActionResult Update(int productId, int quantity)
    {
        _cart.SetQuantity(productId, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Remove(int productId)
    {
        _cart.Remove(productId);
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Back(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction(nameof(Index));
    }
}
