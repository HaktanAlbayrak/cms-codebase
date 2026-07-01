using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Areas.Admin.Models;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Areas.Admin.Controllers;

/// <summary>Makine kategorilerinin yönetimi (dil sekmeli). İçerik modülü.</summary>
public class MachineCategoriesController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;

    public MachineCategoriesController(ApplicationDbContext db, IContentCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _db.MachineCategories
            .Include(c => c.Translations)
            .Include(c => c.Machines)
            .OrderBy(c => c.SortOrder)
            .AsNoTracking().ToListAsync();
        return View(categories);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        await PopulateLanguagesAsync();
        if (id is null) return View(new MachineCategoryEditModel());

        var category = await _db.MachineCategories.Include(c => c.Translations)
            .AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return NotFound();

        var model = new MachineCategoryEditModel
        {
            Id = category.Id,
            Slug = category.Slug,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };
        foreach (var t in category.Translations)
            model.Name[t.LanguageCode] = t.Name;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MachineCategoryEditModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            TempData["Error"] = AdminMessages.MachineCategorySlugRequired;
            await PopulateLanguagesAsync();
            return View(model);
        }

        var category = model.Id == 0
            ? new MachineCategory()
            : await _db.MachineCategories.Include(c => c.Translations).FirstOrDefaultAsync(c => c.Id == model.Id);
        if (category is null) return NotFound();

        category.Slug = AdminSlug.Make(model.Slug);
        category.SortOrder = model.SortOrder;
        category.IsActive = model.IsActive;

        if (model.Id == 0) _db.MachineCategories.Add(category);

        foreach (var lang in await ActiveLanguageCodesAsync())
        {
            var tr = category.Translations.FirstOrDefault(t => t.LanguageCode == lang);
            if (tr is null)
            {
                tr = new MachineCategoryTranslation { LanguageCode = lang };
                category.Translations.Add(tr);
            }
            tr.Name = Val(model.Name, lang);
        }

        await _db.SaveChangesAsync();
        _cache.InvalidateAll();
        TempData["Success"] = AdminMessages.MachineCategorySaved;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.MachineCategories.FindAsync(id);
        if (category is not null)
        {
            _db.MachineCategories.Remove(category);   // makineler cascade silinir
            await _db.SaveChangesAsync();
            _cache.InvalidateAll();
            TempData["Success"] = AdminMessages.MachineCategoryDeleted;
        }
        return RedirectToAction(nameof(Index));
    }

    private static string Val(IDictionary<string, string> map, string lang) =>
        map.TryGetValue(lang, out var v) ? (v ?? "") : "";

    private async Task<List<string>> ActiveLanguageCodesAsync() =>
        await _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder).Select(l => l.Code).ToListAsync();

    private async Task PopulateLanguagesAsync() =>
        ViewBag.Languages = await _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder)
            .AsNoTracking().ToListAsync();
}
