using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Areas.Admin.Models;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Areas.Admin.Controllers;

/// <summary>Hizmetlerin yönetimi (dil sekmeli CRUD). İçerik modülü → ContentManager erişebilir.</summary>
public class ServicesController : AdminControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly IContentCache _cache;

    public ServicesController(ApplicationDbContext db, IFileStorageService files, IContentCache cache)
    {
        _db = db;
        _files = files;
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        var services = await _db.Services
            .Include(s => s.Translations)
            .OrderBy(s => s.SortOrder)
            .AsNoTracking().ToListAsync();
        return View(services);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        await PopulateLanguagesAsync();
        if (id is null) return View(new ServiceEditModel());

        var service = await _db.Services.Include(s => s.Translations)
            .AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (service is null) return NotFound();

        var model = new ServiceEditModel
        {
            Id = service.Id,
            Slug = service.Slug,
            Icon = service.Icon,
            SortOrder = service.SortOrder,
            IsActive = service.IsActive,
            ImageUrl = service.ImageUrl
        };
        foreach (var t in service.Translations)
        {
            model.Title[t.LanguageCode] = t.Title;
            model.Summary[t.LanguageCode] = t.Summary;
            model.Body[t.LanguageCode] = t.Body;
            model.MetaTitle[t.LanguageCode] = t.MetaTitle ?? "";
            model.MetaDescription[t.LanguageCode] = t.MetaDescription ?? "";
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ServiceEditModel model, IFormFile? imageFile)
    {
        if (string.IsNullOrWhiteSpace(model.Slug))
        {
            TempData["Error"] = AdminMessages.ServiceSlugRequired;
            await PopulateLanguagesAsync();
            return View(model);
        }

        var service = model.Id == 0
            ? new Service()
            : await _db.Services.Include(s => s.Translations).FirstOrDefaultAsync(s => s.Id == model.Id);
        if (service is null) return NotFound();

        service.Slug = AdminSlug.Make(model.Slug);
        service.Icon = string.IsNullOrWhiteSpace(model.Icon) ? "construction" : model.Icon.Trim();
        service.SortOrder = model.SortOrder;
        service.IsActive = model.IsActive;

        if (imageFile is { Length: > 0 })
            service.ImageUrl = await _files.SaveAsync(imageFile, "services");
        else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
            service.ImageUrl = model.ImageUrl;

        if (model.Id == 0) _db.Services.Add(service);

        foreach (var lang in await ActiveLanguageCodesAsync())
        {
            var tr = service.Translations.FirstOrDefault(t => t.LanguageCode == lang);
            if (tr is null)
            {
                tr = new ServiceTranslation { LanguageCode = lang };
                service.Translations.Add(tr);
            }
            tr.Title = Val(model.Title, lang);
            tr.Summary = Val(model.Summary, lang);
            tr.Body = Val(model.Body, lang);
            tr.MetaTitle = Val(model.MetaTitle, lang);
            tr.MetaDescription = Val(model.MetaDescription, lang);
        }

        await _db.SaveChangesAsync();
        _cache.InvalidateAll();
        TempData["Success"] = AdminMessages.ServiceSaved;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service is not null)
        {
            _files.Delete(service.ImageUrl);
            _db.Services.Remove(service);
            await _db.SaveChangesAsync();
            _cache.InvalidateAll();
            TempData["Success"] = AdminMessages.ServiceDeleted;
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
