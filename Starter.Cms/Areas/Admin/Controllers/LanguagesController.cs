using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Starter.Cms.Data;
using Starter.Cms.Domain;
using Starter.Cms.Localization;
using Starter.Cms.Services;

namespace Starter.Cms.Areas.Admin.Controllers;

/// <summary>Dil yönetimi. Yeni dil eklendiğinde arayüz metinleri varsayılan dilden tohumlanır.</summary>
public class LanguagesController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IContentCache _cache;
    private readonly ILocalizationStore _locStore;

    public LanguagesController(ApplicationDbContext db, IContentCache cache, ILocalizationStore locStore)
    {
        _db = db;
        _cache = cache;
        _locStore = locStore;
    }

    public async Task<IActionResult> Index()
    {
        var languages = await _db.Languages.OrderBy(l => l.SortOrder).AsNoTracking().ToListAsync();
        return View(languages);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, string code, string name, int sortOrder, bool isRtl, bool isDefault, bool isActive)
    {
        code = (code ?? "").Trim().ToLowerInvariant();
        if (code.Length != 2 || !code.All(char.IsLetter))
        {
            TempData["Error"] = AdminMessages.InvalidCultureCode;
            return RedirectToAction(nameof(Index));
        }

        var isNew = id == 0;
        var lang = isNew ? new Language() : await _db.Languages.FindAsync(id);
        if (lang is null) return NotFound();

        lang.Code = code;
        lang.Name = string.IsNullOrWhiteSpace(name) ? code.ToUpperInvariant() : name.Trim();
        lang.SortOrder = sortOrder;
        lang.IsRtl = isRtl;
        lang.IsActive = isActive || isDefault; // varsayılan her zaman aktif
        if (isNew) _db.Languages.Add(lang);

        // Tek varsayılan dil garantisi.
        if (isDefault)
        {
            foreach (var other in await _db.Languages.Where(l => l.Id != lang.Id).ToListAsync())
                other.IsDefault = false;
            lang.IsDefault = true;
        }
        else if (!await _db.Languages.AnyAsync(l => l.IsDefault && l.Id != lang.Id))
        {
            lang.IsDefault = true; // hiç varsayılan kalmazsa bunu varsayılan yap
        }

        await _db.SaveChangesAsync();

        // Yeni dilin arayüz metinlerini varsayılan dilden tohumla (idempotent).
        var seeded = false;
        if (isNew)
            seeded = await SeedLocalizationFromDefaultAsync(code);

        await ReconfigureAsync();
        TempData["Success"] = seeded ? AdminMessages.LanguageAddedSeeded : AdminMessages.LanguageSaved;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lang = await _db.Languages.FindAsync(id);
        if (lang is null) return RedirectToAction(nameof(Index));
        if (lang.IsDefault)
        {
            TempData["Error"] = AdminMessages.DefaultLanguageCannotBeDeleted;
            return RedirectToAction(nameof(Index));
        }

        var resources = await _db.LocalizationResources.Where(r => r.LanguageCode == lang.Code).ToListAsync();
        _db.LocalizationResources.RemoveRange(resources);
        _db.Languages.Remove(lang);
        await _db.SaveChangesAsync();

        await ReconfigureAsync();
        TempData["Success"] = AdminMessages.LanguageDeleted;
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> SeedLocalizationFromDefaultAsync(string newCode)
    {
        var defaultCode = CultureContext.Default;
        if (string.Equals(defaultCode, newCode, StringComparison.OrdinalIgnoreCase)) return false;

        var defaults = await _db.LocalizationResources.Where(r => r.LanguageCode == defaultCode).ToListAsync();
        var existingKeys = await _db.LocalizationResources
            .Where(r => r.LanguageCode == newCode).Select(r => r.Key).ToListAsync();
        var have = new HashSet<string>(existingKeys);

        var added = false;
        foreach (var r in defaults)
        {
            if (have.Contains(r.Key)) continue;
            _db.LocalizationResources.Add(new LocalizationResource { Key = r.Key, LanguageCode = newCode, Value = r.Value });
            added = true;
        }
        if (added) await _db.SaveChangesAsync();
        return added;
    }

    /// <summary>DB'deki güncel dillerle CultureContext'i ve cache'leri tazeler.</summary>
    private async Task ReconfigureAsync()
    {
        var langs = await _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder)
            .Select(l => new { l.Code, l.IsRtl, l.IsDefault }).ToListAsync();
        CultureContext.Configure(langs.Select(l => (l.Code, l.IsRtl, l.IsDefault)));
        _cache.InvalidateAll();
        _locStore.Invalidate();
    }
}
