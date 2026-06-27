using Microsoft.AspNetCore.Mvc;
using Starter.Cms.Services;

namespace Starter.Cms.Areas.Admin.Controllers;

/// <summary>Marka, tema rengi, iletişim, sosyal, SEO ve logo ayarları (sıfır statik).</summary>
public class BrandingController : AdminControllerBase
{
    private readonly IBrandingService _branding;
    private readonly ISiteSettingService _settings;
    private readonly IFileStorageService _files;

    public BrandingController(IBrandingService branding, ISiteSettingService settings, IFileStorageService files)
    {
        _branding = branding;
        _settings = settings;
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Settings = await _settings.GetAllAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Index(
        Dictionary<string, string> values,
        IFormFile? logoFile,
        IFormFile? logoLightFile,
        IFormFile? faviconFile)
    {
        values ??= new();

        if (logoFile is { Length: > 0 }) values["branding.logoUrl"] = await _files.SaveAsync(logoFile, "branding");
        if (logoLightFile is { Length: > 0 }) values["branding.logoLightUrl"] = await _files.SaveAsync(logoLightFile, "branding");
        if (faviconFile is { Length: > 0 }) values["branding.faviconUrl"] = await _files.SaveAsync(faviconFile, "branding");

        // Boş gönderilen logo alanları mevcut değeri ezmemeli (dosya yoksa anahtarı atla).
        foreach (var k in new[] { "branding.logoUrl", "branding.logoLightUrl", "branding.faviconUrl" })
            if (values.TryGetValue(k, out var v) && string.IsNullOrWhiteSpace(v))
                values.Remove(k);

        await _branding.SaveAsync(values);
        TempData["Success"] = AdminMessages.BrandingSaved;
        return RedirectToAction(nameof(Index));
    }
}
