using HandmadeStore.Data;
using HandmadeStore.Models;
using HandmadeStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Areas.Admin.Controllers;

public class AdminCustomerRow
{
    public ApplicationUser Customer { get; set; } = new();
    public int OrderCount { get; set; }
}

public class AdminCustomerListViewModel
{
    public List<AdminCustomerRow> Rows { get; set; } = new();
    public string? Q { get; set; }
    public int Total { get; set; }
    public PagerViewModel Pager { get; set; } = new();
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CustomersController : Controller
{
    private const int PageSize = 15;

    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var customerRoleId = await _db.Roles.Where(r => r.Name == Roles.Customer).Select(r => r.Id).FirstOrDefaultAsync();
        var query = _db.Users.AsNoTracking()
            .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == customerRoleId));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(c => c.Name.Contains(term)
                                     || (c.PhoneNumber != null && c.PhoneNumber.Contains(term))
                                     || (c.Email != null && c.Email.Contains(term)));
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var route = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(q))
        {
            route["q"] = q.Trim();
        }

        var rows = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(c => new AdminCustomerRow { Customer = c, OrderCount = c.Orders.Count() })
            .ToListAsync();

        return View(new AdminCustomerListViewModel
        {
            Rows = rows,
            Q = q,
            Total = total,
            Pager = new PagerViewModel { Page = page, TotalPages = totalPages, Action = "Index", Route = route }
        });
    }
}
