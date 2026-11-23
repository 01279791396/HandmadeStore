using HandmadeStore.Data;
using HandmadeStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Areas.Admin.Controllers;

public class DashboardStats
{
    public int Products { get; set; }
    public int Customers { get; set; }
    public int Categories { get; set; }
    public int Orders { get; set; }
    public int NewOrders { get; set; }
    public decimal Revenue { get; set; }
    public List<Order> Recent { get; set; } = new();
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        // الجمع بيتم في الذاكرة لتبسيط الاستعلام
        var delivered = await _db.Orders.AsNoTracking()
            .Where(o => o.Status == OrderStatus.Delivered)
            .Select(o => o.Total)
            .ToListAsync();

        var stats = new DashboardStats
        {
            Products = await _db.Products.CountAsync(),
            Customers = await _db.UserRoles.CountAsync(ur => ur.RoleId ==
                _db.Roles.Where(r => r.Name == Roles.Customer).Select(r => r.Id).FirstOrDefault()),
            Categories = await _db.Categories.CountAsync(),
            Orders = await _db.Orders.CountAsync(),
            NewOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.New),
            Revenue = delivered.Sum(),
            Recent = await _db.Orders.AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .Take(6)
                .ToListAsync()
        };

        return View(stats);
    }
}
