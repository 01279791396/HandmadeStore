using HandmadeStore.Data;
using HandmadeStore.Models;
using HandmadeStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Areas.Admin.Controllers;

public class AdminOrderListViewModel
{
    public List<Order> Orders { get; set; } = new();
    public OrderStatus? Status { get; set; }
    public PagerViewModel Pager { get; set; } = new();
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private const int PageSize = 15;

    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(OrderStatus? status, int page = 1)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var route = new Dictionary<string, string>();
        if (status.HasValue)
        {
            route["status"] = ((int)status.Value).ToString();
        }

        var vm = new AdminOrderListViewModel
        {
            Status = status,
            Orders = await query.OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(),
            Pager = new PagerViewModel { Page = page, TotalPages = totalPages, Action = "Index", Route = route }
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    /// <summary>عرض/طباعة الفاتورة (لازم تكون اتصدرت الأول)</summary>
    public async Task<IActionResult> Invoice(int id)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        if (!order.HasInvoice)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        return View("~/Views/Shared/Invoice.cshtml", order);
    }

    /// <summary>إصدار فاتورة للطلب: بيتحط ليها رقم وتظهر للعميل في حسابه</summary>
    [HttpPost]
    public async Task<IActionResult> IssueInvoice(int id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            return NotFound();
        }

        if (!order.HasInvoice)
        {
            var issued = DateTime.UtcNow;
            order.InvoiceNumber = $"INV-{issued:yyyy}-{order.Id:D5}";
            order.InvoiceIssuedAt = issued;
            await _db.SaveChangesAsync();
            TempData["Success"] = "تم إصدار الفاتورة وهتظهر للعميل في حسابه";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        if (!Enum.IsDefined(typeof(OrderStatus), status))
        {
            return BadRequest();
        }

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            return NotFound();
        }

        order.Status = status;
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم تحديث حالة الطلب";
        return RedirectToAction(nameof(Details), new { id });
    }
}
