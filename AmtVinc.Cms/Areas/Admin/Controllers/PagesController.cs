using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Areas.Admin.Models;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Areas.Admin.Controllers;

public class PagesController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly IContentCache _cache;

    public PagesController(ApplicationDbContext db, IFileStorageService files, IContentCache cache)
    {
        _db = db;
        _files = files;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        var pages = await _db.Pages
            .Include(p => p.Translations)
            .OrderBy(p => p.SortOrder)
            .AsNoTracking().ToListAsync();
        return View(pages);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        await PopulateLanguagesAsync();
        if (id is null) return View(new PageEditModel());

        var page = await _db.Pages.Include(p => p.Translations)
            .AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (page is null) return NotFound();

        var model = new PageEditModel
        {
            Id = page.Id,
            Slug = page.Slug,
            LayoutKey = page.LayoutKey,
            SortOrder = page.SortOrder,
            IsActive = page.IsActive,
            CoverImageUrl = page.CoverImageUrl
        };
        foreach (var t in page.Translations)
        {
            model.Title[t.LanguageCode] = t.Title;
            model.Lead[t.LanguageCode] = t.Lead;
            model.Body[t.LanguageCode] = t.Body;
            model.MetaTitle[t.LanguageCode] = t.MetaTitle ?? "";
            model.MetaDescription[t.LanguageCode] = t.MetaDescription ?? "";
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PageEditModel model, IFormFile? coverFile)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            TempData["Error"] = "Slug (URL adı) zorunludur.";
            await PopulateLanguagesAsync();
            return View(model);
        }

        var page = model.Id == 0
            ? new Page()
            : await _db.Pages.Include(p => p.Translations).FirstOrDefaultAsync(p => p.Id == model.Id);
        if (page is null) return NotFound();

        page.Slug = Slugify(model.Slug);
        page.LayoutKey = string.IsNullOrWhiteSpace(model.LayoutKey) ? "standard" : model.LayoutKey;
        page.SortOrder = model.SortOrder;
        page.IsActive = model.IsActive;

        if (coverFile is { Length: > 0 })
            page.CoverImageUrl = await _files.SaveAsync(coverFile, "pages");
        else if (!string.IsNullOrWhiteSpace(model.CoverImageUrl))
            page.CoverImageUrl = model.CoverImageUrl;

        if (model.Id == 0) _db.Pages.Add(page);

        var languages = await ActiveLanguageCodesAsync();
        foreach (var lang in languages)
        {
            var tr = page.Translations.FirstOrDefault(t => t.LanguageCode == lang);
            if (tr is null)
            {
                tr = new PageTranslation { LanguageCode = lang };
                page.Translations.Add(tr);
            }
            tr.Title = Val(model.Title, lang);
            tr.Lead = Val(model.Lead, lang);
            tr.Body = Val(model.Body, lang);
            tr.MetaTitle = Val(model.MetaTitle, lang);
            tr.MetaDescription = Val(model.MetaDescription, lang);
        }

        await _db.SaveChangesAsync();
        _cache.InvalidateAll();
        TempData["Success"] = AdminMessages.PageSaved;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var page = await _db.Pages.FindAsync(id);
        if (page is not null)
        {
            _files.Delete(page.CoverImageUrl);
            _db.Pages.Remove(page);
            await _db.SaveChangesAsync();
            _cache.InvalidateAll();
            TempData["Success"] = AdminMessages.PageDeleted;
        }
        return RedirectToAction(nameof(Index));
    }

    private static string Val(IDictionary<string, string> map, string lang) =>
        map.TryGetValue(lang, out var v) ? (v ?? "") : "";

    private static string Slugify(string value)
    {
        var slug = new string(value.Trim().ToLowerInvariant()
            .Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u').Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c')
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        return string.Join('-', slug.Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private async Task<List<string>> ActiveLanguageCodesAsync() =>
        await _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder).Select(l => l.Code).ToListAsync();

    private async Task PopulateLanguagesAsync() =>
        ViewBag.Languages = await _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder)
            .AsNoTracking().ToListAsync();
}
