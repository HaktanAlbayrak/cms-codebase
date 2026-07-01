using Microsoft.AspNetCore.Mvc;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Controllers;

/// <summary>Genel içerik sayfalarını slug ile gösterir (Hakkımızda, gizlilik, vb.).</summary>
public class PagesController : Controller
{
    private readonly IPageService _pages;

    public PagesController(IPageService pages) => _pages = pages;

    public async Task<IActionResult> Detail(string slug, string culture)
    {
        if (!CultureContext.IsSupported(culture)) culture = CultureContext.Current;

        var page = await _pages.GetBySlugAsync(slug, culture);
        if (page is null) return NotFound();

        return View(page);
    }
}
