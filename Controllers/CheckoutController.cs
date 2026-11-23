using HandmadeStore.Data;
using HandmadeStore.Helpers;
using HandmadeStore.Models;
using HandmadeStore.Services;
using HandmadeStore.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HandmadeStore.Controllers;

public class CheckoutController : Controller
{
    private readonly AppDbContext _db;
    private readonly CartService _cart;
    private readonly StoreSettings _store;

    public CheckoutController(AppDbContext db, CartService cart, IOptions<StoreSettings> store)
    {
        _db = db;
        _cart = cart;
        _store = store.Value;
    }

    private void FillSettings(CheckoutViewModel vm, CartViewModel cart)
    {
        vm.Cart = cart;
        vm.DeliveryFee = _store.DeliveryFee;
        vm.FreeDeliveryOver = _store.FreeDeliveryOver;
        vm.PickupEnabled = _store.PickupEnabled;
        vm.StoreAddress = _store.Address;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await _cart.BuildAsync();
        if (cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        var vm = new CheckoutViewModel();
        FillSettings(vm, cart);

        var customerId = CurrentCustomerId();
        if (customerId != null)
        {
            var customer = await _db.Users.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer != null)
            {
                vm.CustomerName = customer.Name;
                vm.Phone = customer.Phone;
                vm.Address = customer.Address ?? "";
            }
        }

        return View(vm);
    }

    private string? CurrentCustomerId() =>
        User.IsInRole(Roles.Customer) ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

    [HttpPost]
    public async Task<IActionResult> Index(CheckoutViewModel vm)
    {
        var cart = await _cart.BuildAsync();
        if (cart.Items.Count == 0)
        {
            return RedirectToAction("Index", "Cart");
        }

        var unavailable = cart.Items.Where(i => !i.Product.IsAvailable).ToList();
        if (unavailable.Count > 0)
        {
            foreach (var item in unavailable)
            {
                _cart.Remove(item.Product.Id);
            }

            TempData["Error"] = "بعض المنتجات في السلة لم تعد متاحة وتم حذفها: "
                                + string.Join("، ", unavailable.Select(i => i.Product.Name));
            return RedirectToAction("Index", "Cart");
        }

        // الاستلام من المتجر لازم يكون مفعّل في الإعدادات
        if (vm.DeliveryMethod == DeliveryMethod.Pickup && !_store.PickupEnabled)
        {
            vm.DeliveryMethod = DeliveryMethod.Delivery;
        }

        if (vm.DeliveryMethod == DeliveryMethod.Delivery && string.IsNullOrWhiteSpace(vm.Address))
        {
            ModelState.AddModelError(nameof(vm.Address), "العنوان مطلوب للتوصيل");
        }

        if (!ModelState.IsValid)
        {
            FillSettings(vm, cart);
            return View(vm);
        }

        var isPickup = vm.DeliveryMethod == DeliveryMethod.Pickup;
        var subtotal = cart.Total;
        var fee = isPickup ? 0m : _store.CalcDeliveryFee(subtotal);

        string code;
        do
        {
            code = OrderCode.Generate();
        } while (await _db.Orders.AnyAsync(o => o.Code == code));

        var customerId = CurrentCustomerId();
        var customer = customerId == null
            ? null
            : await _db.Users.FirstOrDefaultAsync(c => c.Id == customerId);

        var order = new Order
        {
            Code = code,
            CustomerId = customer?.Id,
            CustomerName = vm.CustomerName.Trim(),
            Phone = Fmt.NormalizePhone(vm.Phone),
            Address = isPickup ? "استلام من المتجر" : vm.Address.Trim(),
            DeliveryMethod = vm.DeliveryMethod,
            DeliveryFee = fee,
            Notes = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim(),
            Status = OrderStatus.New,
            CreatedAt = DateTime.UtcNow,
            Total = subtotal + fee
        };

        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.Product.Id,
                ProductName = item.Product.Name,
                UnitPrice = item.Product.Price,
                Quantity = item.Quantity
            });
        }

        if (customer != null && !isPickup && string.IsNullOrWhiteSpace(customer.Address))
        {
            customer.Address = order.Address;
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        _cart.Clear();

        return RedirectToAction(nameof(Success), new { code = order.Code });
    }

    public async Task<IActionResult> Success(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return NotFound();
        }

        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Code == code);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }
}
