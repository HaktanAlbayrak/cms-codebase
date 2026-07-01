using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Areas.Admin.Models;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Areas.Admin.Controllers;

public class SlidesController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly IContentCache _cache;

    public SlidesController(ApplicationDbContext db, IFileStorageService files, IContentCache cache)
    {
        _db = db;
        _files = files;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        var slides = await _db.Slides
            .Include(s => s.Translations)
            .OrderBy(s => s.SortOrder)
            .AsNoTracking().ToListAsync();
        return View(slides);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        await PopulateLanguagesAsync();
        if (id is null) return View(new SlideEditModel());

        var slide = await _db.Slides.Include(s => s.Translations)
            .AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (slide is null) return NotFound();

        var model = new SlideEditModel
        {
            Id = slide.Id,
            SortOrder = slide.SortOrder,
            IsActive = slide.IsActive,
            ImageDesktop = slide.ImageDesktop,
            ImageMobile = slide.ImageMobile
        };
        foreach (var t in slide.Translations)
        {
            model.Kicker[t.LanguageCode] = t.Kicker;
            model.Title[t.LanguageCode] = t.Title;
            model.Subtitle[t.LanguageCode] = t.Subtitle;
            model.CtaText[t.LanguageCode] = t.CtaText;
            model.CtaUrl[t.LanguageCode] = t.CtaUrl;
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SlideEditModel model, IFormFile? desktopFile, IFormFile? mobileFile)
    {
        var slide = model.Id == 0
            ? new Slide()
            : await _db.Slides.Include(s => s.Translations).FirstOrDefaultAsync(s => s.Id == model.Id);
        if (slide is null) return NotFound();

        if (desktopFile is { Length: > 0 })
            slide.ImageDesktop = await _files.SaveAsync(desktopFile, "slides");
        else if (!string.IsNullOrWhiteSpace(model.ImageDesktop))
            slide.ImageDesktop = model.ImageDesktop;

        if (mobileFile is { Length: > 0 })
            slide.ImageMobile = await _files.SaveAsync(mobileFile, "slides");
        else if (!string.IsNullOrWhiteSpace(model.ImageMobile))
            slide.ImageMobile = model.ImageMobile;

        if (string.IsNullOrWhiteSpace(slide.ImageDesktop))
        {
            TempData["Error"] = AdminMessages.SlideImageRequired;
            await PopulateLanguagesAsync();
            return View(model);
        }

        slide.SortOrder = model.SortOrder;
        slide.IsActive = model.IsActive;
        if (model.Id == 0) _db.Slides.Add(slide);

        foreach (var lang in await ActiveLanguageCodesAsync())
        {
            var tr = slide.Translations.FirstOrDefault(t => t.LanguageCode == lang);
            if (tr is null)
            {
                tr = new SlideTranslation { LanguageCode = lang };
                slide.Translations.Add(tr);
            }
            tr.Kicker = Val(model.Kicker, lang);
            tr.Title = Val(model.Title, lang);
            tr.Subtitle = Val(model.Subtitle, lang);
            tr.CtaText = Val(model.CtaText, lang);
            tr.CtaUrl = Val(model.CtaUrl, lang);
        }

        await _db.SaveChangesAsync();
        _cache.InvalidateAll();
        TempData["Success"] = AdminMessages.SlideSaved;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var slide = await _db.Slides.FindAsync(id);
        if (slide is not null)
        {
            _db.Slides.Remove(slide);
            await _db.SaveChangesAsync();
            _cache.InvalidateAll();
            TempData["Success"] = AdminMessages.SlideDeleted;
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
