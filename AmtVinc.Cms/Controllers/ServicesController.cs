using Microsoft.AspNetCore.Mvc;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Controllers;

/// <summary>Hizmetler listesi ve hizmet detay sayfası.</summary>
public class ServicesController : Controller
{
    private readonly IServiceCatalogService _services;

    public ServicesController(IServiceCatalogService services) => _services = services;

    public async Task<IActionResult> Index()
    {
        var culture = CultureContext.Current;
        var list = await _services.GetAllAsync(culture);
        return View(list);
    }

    public async Task<IActionResult> Detail(string slug, string culture)
    {
        if (!CultureContext.IsSupported(culture)) culture = CultureContext.Current;

        var service = await _services.GetBySlugAsync(slug, culture);
        if (service is null) return NotFound();

        ViewData["Title"] = service.MetaTitle;
        ViewData["MetaDescription"] = service.MetaDescription;
        return View(service);
    }
}
