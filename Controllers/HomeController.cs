using HandmadeStore.Data;
using HandmadeStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new HomeViewModel
        {
            Featured = await _db.Products.AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.IsAvailable && p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync(),

            Latest = await _db.Products.AsNoTracking()
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync(),

            Categories = await _db.Categories.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new CategoryCardViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Count = c.Products.Count()
                })
                .ToListAsync()
        };

        return View(vm);
    }

    public IActionResult About() => View();

    [AllowAnonymous]
    public IActionResult Status(int code)
    {
        ViewData["Code"] = code;
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
