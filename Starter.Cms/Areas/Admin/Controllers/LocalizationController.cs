using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Cms.Data;
using Starter.Cms.Domain;
using Starter.Cms.Localization;

namespace Starter.Cms.Areas.Admin.Controllers;

/// <summary>Arayüz metinleri (IStringLocalizer kaynağı) — dil sekmeli, tek formda tüm diller.</summary>
public class LocalizationController : AdminOnlyControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILocalizationStore _store;

    public LocalizationController(ApplicationDbContext db, ILocalizationStore store)
    {
        _db = db;
        _store = store;
    }

    public async Task<IActionResult> Index()
    {
        var languages = await _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder)
            .AsNoTracking().ToListAsync();
        var resources = await _db.LocalizationResources.AsNoTracking().ToListAsync();

        // key → (lang → value)
        var keys = resources.Select(r => r.Key).Distinct().OrderBy(k => k).ToList();
        var map = resources.GroupBy(r => r.Key)
            .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.LanguageCode, r => r.Value));

        ViewBag.Languages = languages;
        ViewBag.Keys = keys;
        ViewBag.Map = map;
        return View();
    }

    /// <summary>values[key][lang] = değer — tüm diller tek seferde upsert edilir.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(Dictionary<string, Dictionary<string, string>> values)
    {
        values ??= new();
        var all = await _db.LocalizationResources.ToListAsync();
        var index = all.ToDictionary(r => (r.Key, r.LanguageCode));

        foreach (var (key, perLang) in values)
        {
            if (string.IsNullOrWhiteSpace(key) || perLang is null) continue;
            foreach (var (lang, value) in perLang)
            {
                if (index.TryGetValue((key, lang), out var existing))
                    existing.Value = value ?? "";
                else
                    _db.LocalizationResources.Add(new LocalizationResource { Key = key, LanguageCode = lang, Value = value ?? "" });
            }
        }

        await _db.SaveChangesAsync();
        _store.Invalidate();
        TempData["Success"] = AdminMessages.TranslationsSaved;
        return RedirectToAction(nameof(Index));
    }
}
