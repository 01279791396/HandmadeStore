using HandmadeStore.Data;
using HandmadeStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Areas.Admin.Controllers;

public class AdminCategoryRow
{
    public Category Category { get; set; } = new();
    public int ProductCount { get; set; }
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var rows = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new AdminCategoryRow { Category = c, ProductCount = c.Products.Count() })
            .ToListAsync();

        return View(rows);
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new Category());

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            return NotFound();
        }

        return View("Form", category);
    }

    [HttpPost]
    public async Task<IActionResult> Save([Bind("Id,Name,Description")] Category model)
    {
        var name = (model.Name ?? "").Trim();

        if (await _db.Categories.AnyAsync(c => c.Name == name && c.Id != model.Id))
        {
            ModelState.AddModelError(nameof(model.Name), "التصنيف ده موجود بالفعل");
        }

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        if (model.Id == 0)
        {
            _db.Categories.Add(new Category { Name = name, Description = model.Description?.Trim() });
            TempData["Success"] = "تمت إضافة التصنيف";
        }
        else
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == model.Id);
            if (category == null)
            {
                return NotFound();
            }

            category.Name = name;
            category.Description = model.Description?.Trim();
            TempData["Success"] = "تم حفظ التعديلات";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category == null)
        {
            return NotFound();
        }

        if (await _db.Products.AnyAsync(p => p.CategoryId == id))
        {
            TempData["Error"] = "مينفعش تحذف تصنيف فيه منتجات. انقل المنتجات أو احذفها الأول.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم حذف التصنيف";
        return RedirectToAction(nameof(Index));
    }
}
