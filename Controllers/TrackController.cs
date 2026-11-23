using HandmadeStore.Data;
using HandmadeStore.Helpers;
using HandmadeStore.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace HandmadeStore.Controllers;

public class TrackController : Controller
{
    private readonly AppDbContext _db;

    public TrackController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index(string? code) => View(new TrackViewModel { Code = code ?? "" });

    [HttpPost]
    [EnableRateLimiting("lookup")]
    public async Task<IActionResult> Index(TrackViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var code = OrderCode.Normalize(vm.Code);
        var phone = Fmt.NormalizePhone(vm.Phone);

        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Code == code && o.Phone == phone);

        vm.Searched = true;
        vm.Result = order;
        return View(vm);
    }
}
