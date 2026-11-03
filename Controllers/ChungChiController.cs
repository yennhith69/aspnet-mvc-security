using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Policy = Permissions.HoSoXem)]
public class ChungChiController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly HoSoScopeService _scope;

    public ChungChiController(ApplicationDbContext db, HoSoScopeService scope)
    {
        _db = db;
        _scope = scope;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int nhanSuId)
    {
        var ns = await (await _scope.ScopedAsync(User)).AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == nhanSuId);
        if (ns == null) return NotFound();
        return View(new ChungChiViewModel
        {
            NhanSuId = ns.Id,
            HoTen = ns.HoTen,
            Items = await _db.ChungChis.AsNoTracking()
                .Where(x => x.NhanSuId == nhanSuId)
                .OrderByDescending(x => x.NgayCap)
                .ToListAsync()
        });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChungChiViewModel vm)
    {
        if (!await _scope.CanViewAsync(User, vm.NhanSuId)) return NotFound();
        if (!ModelState.IsValid)
        {
            vm.HoTen = await _db.NhanSus.Where(x => x.Id == vm.NhanSuId)
                .Select(x => x.HoTen).FirstOrDefaultAsync() ?? "";
            vm.Items = await _db.ChungChis.AsNoTracking().Where(x => x.NhanSuId == vm.NhanSuId)
                .OrderByDescending(x => x.NgayCap).ToListAsync();
            return View("Index", vm);
        }
        _db.ChungChis.Add(new ChungChi
        {
            NhanSuId = vm.NhanSuId,
            Ten = vm.Ten.Trim(),
            DonViCap = string.IsNullOrWhiteSpace(vm.DonViCap) ? null : vm.DonViCap.Trim(),
            NgayCap = vm.NgayCap,
            SoHieu = string.IsNullOrWhiteSpace(vm.SoHieu) ? null : vm.SoHieu.Trim()
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã thêm chứng chỉ.";
        return RedirectToAction(nameof(Index), new { nhanSuId = vm.NhanSuId });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.HoSoSua)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.ChungChis.FindAsync(id);
        if (item == null || !await _scope.CanViewAsync(User, item.NhanSuId)) return NotFound();
        _db.ChungChis.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa chứng chỉ.";
        return RedirectToAction(nameof(Index), new { nhanSuId = item.NhanSuId });
    }
}
