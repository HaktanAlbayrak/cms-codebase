using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Areas.Admin.Models;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Areas.Admin.Controllers;

/// <summary>Makine filosunun yönetimi (dil sekmeli + teknik özellikler + kategori). İçerik modülü.</summary>
public class MachinesController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly IContentCache _cache;

    public MachinesController(ApplicationDbContext db, IFileStorageService files, IContentCache cache)
    {
        _db = db;
        _files = files;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        var machines = await _db.Machines
            .Include(m => m.Translations)
            .Include(m => m.Category!).ThenInclude(c => c.Translations)
            .OrderBy(m => m.MachineCategoryId).ThenBy(m => m.SortOrder)
            .AsNoTracking().ToListAsync();
        return View(machines);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        // Makine eklemeden önce en az bir kategori gerekir.
        if (!await _db.MachineCategories.AnyAsync())
        {
            TempData["Error"] = AdminMessages.MachineCategoryFirst;
            return RedirectToAction("Index", "MachineCategories");
        }

        await PopulateAsync();
        if (id is null) return View(new MachineEditModel());

        var machine = await _db.Machines.Include(m => m.Translations)
            .AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (machine is null) return NotFound();

        var model = new MachineEditModel
        {
            Id = machine.Id,
            MachineCategoryId = machine.MachineCategoryId,
            Slug = machine.Slug,
            SortOrder = machine.SortOrder,
            IsActive = machine.IsActive,
            IsFeatured = machine.IsFeatured,
            ImageUrl = machine.ImageUrl,
            WorkingHeight = machine.WorkingHeight,
            Capacity = machine.Capacity,
            Reach = machine.Reach,
            Weight = machine.Weight
        };
        foreach (var t in machine.Translations)
        {
            model.Name[t.LanguageCode] = t.Name;
            model.ShortDescription[t.LanguageCode] = t.ShortDescription;
            model.Description[t.LanguageCode] = t.Description;
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MachineEditModel model, IFormFile? imageFile)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            TempData["Error"] = AdminMessages.MachineSlugRequired;
            await PopulateAsync();
            return View(model);
        }

        var machine = model.Id == 0
            ? new Machine()
            : await _db.Machines.Include(m => m.Translations).FirstOrDefaultAsync(m => m.Id == model.Id);
        if (machine is null) return NotFound();

        machine.MachineCategoryId = model.MachineCategoryId;
        machine.Slug = AdminSlug.Make(model.Slug);
        machine.SortOrder = model.SortOrder;
        machine.IsActive = model.IsActive;
        machine.IsFeatured = model.IsFeatured;
        machine.WorkingHeight = model.WorkingHeight?.Trim() ?? "";
        machine.Capacity = model.Capacity?.Trim() ?? "";
        machine.Reach = model.Reach?.Trim() ?? "";
        machine.Weight = model.Weight?.Trim() ?? "";

        if (imageFile is { Length: > 0 })
            machine.ImageUrl = await _files.SaveAsync(imageFile, "machines");
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
            machine.ImageUrl = model.ImageUrl;

        if (model.Id == 0) _db.Machines.Add(machine);

        foreach (var lang in await ActiveLanguageCodesAsync())
        {
            var tr = machine.Translations.FirstOrDefault(t => t.LanguageCode == lang);
            if (tr is null)
            {
                tr = new MachineTranslation { LanguageCode = lang };
                machine.Translations.Add(tr);
            }
            tr.Name = Val(model.Name, lang);
            tr.ShortDescription = Val(model.ShortDescription, lang);
            tr.Description = Val(model.Description, lang);
        }

        await _db.SaveChangesAsync();
        _cache.InvalidateAll();
        TempData["Success"] = AdminMessages.MachineSaved;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var machine = await _db.Machines.FindAsync(id);
        if (machine is not null)
        {
            _files.Delete(machine.ImageUrl);
            _db.Machines.Remove(machine);
            await _db.SaveChangesAsync();
            _cache.InvalidateAll();
            TempData["Success"] = AdminMessages.MachineDeleted;
        }
        return RedirectToAction(nameof(Index));
    }

    private static string Val(IDictionary<string, string> map, string lang) =>
        map.TryGetValue(lang, out var v) ? (v ?? "") : "";

    private async Task<List<string>> ActiveLanguageCodesAsync() =>
        await _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder).Select(l => l.Code).ToListAsync();

    private async Task PopulateAsync()
    {
        ViewBag.Languages = await _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder)
            .AsNoTracking().ToListAsync();

        var defaultCode = CultureContext.Default;
        var categories = await _db.MachineCategories
            .Include(c => c.Translations)
            .OrderBy(c => c.SortOrder)
            .AsNoTracking().ToListAsync();

        ViewBag.Categories = categories.Select(c =>
        {
            var t = c.Translations.FirstOrDefault(x => x.LanguageCode == defaultCode) ?? c.Translations.FirstOrDefault();
            return new SelectListItem { Value = c.Id.ToString(), Text = t?.Name ?? c.Slug };
        }).ToList();
    }
}
