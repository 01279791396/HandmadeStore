using HandmadeStore.Data;
using HandmadeStore.Models;
using HandmadeStore.Services;
using HandmadeStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Areas.Admin.Controllers;

public class AdminProductListViewModel
{
    public List<Product> Products { get; set; } = new();
    public string? Q { get; set; }
    public PagerViewModel Pager { get; set; } = new();
}

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private const int PageSize = 15;

    private readonly AppDbContext _db;
    private readonly ImageService _images;

    public ProductsController(AppDbContext db, ImageService images)
    {
        _db = db;
        _images = images;
    }

    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var query = _db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p => p.Name.Contains(term));
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var route = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(q))
        {
            route["q"] = q.Trim();
        }

        var vm = new AdminProductListViewModel
        {
            Q = q,
            Products = await query.OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(),
            Pager = new PagerViewModel { Page = page, TotalPages = totalPages, Action = "Index", Route = route }
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!await _db.Categories.AnyAsync())
        {
            TempData["Error"] = "ضيف تصنيف الأول قبل إضافة منتجات";
            return RedirectToAction("Index", "Categories");
        }

        var vm = new ProductFormViewModel { IsAvailable = true };
        await LoadCategoriesAsync(vm.CategoryId);
        return View("Form", vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        var vm = new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CategoryId = product.CategoryId,
            PrepDays = product.PrepDays,
            IsAvailable = product.IsAvailable,
            IsFeatured = product.IsFeatured,
            ExistingImage = product.ImageFileName
        };

        await LoadCategoriesAsync(vm.CategoryId);
        return View("Form", vm);
    }

    [HttpPost]
    public async Task<IActionResult> Save(ProductFormViewModel vm)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId))
        {
            ModelState.AddModelError(nameof(vm.CategoryId), "اختار تصنيف صحيح");
        }

        Product? product = null;
        if (vm.Id != 0)
        {
            product = await _db.Products.FirstOrDefaultAsync(p => p.Id == vm.Id);
            if (product == null)
            {
                return NotFound();
            }
        }

        string? newImage = null;
        if (vm.Image != null && vm.Image.Length > 0 && ModelState.IsValid)
        {
            var result = await _images.SaveAsync(vm.Image);
            if (result.Error != null)
            {
                ModelState.AddModelError(nameof(vm.Image), result.Error);
            }
            else
            {
                newImage = result.FileName;
            }
        }

        if (!ModelState.IsValid)
        {
            vm.ExistingImage = product?.ImageFileName;
            await LoadCategoriesAsync(vm.CategoryId);
            return View("Form", vm);
        }

        var isNew = product == null;
        if (product == null)
        {
            product = new Product { CreatedAt = DateTime.UtcNow };
            _db.Products.Add(product);
        }

        product.Name = vm.Name.Trim();
        product.Description = vm.Description.Trim();
        product.Price = vm.Price;
        product.CategoryId = vm.CategoryId;
        product.PrepDays = vm.PrepDays;
        product.IsAvailable = vm.IsAvailable;
        product.IsFeatured = vm.IsFeatured;

        if (newImage != null)
        {
            _images.Delete(product.ImageFileName);
            product.ImageFileName = newImage;
        }
        else if (vm.RemoveImage)
        {
            _images.Delete(product.ImageFileName);
            product.ImageFileName = null;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = isNew ? "تمت إضافة المنتج" : "تم حفظ التعديلات";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }

        _images.Delete(product.ImageFileName);
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم حذف المنتج";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategoriesAsync(int selectedId)
    {
        var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(categories, "Id", "Name", selectedId);
    }
}
