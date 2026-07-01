using Microsoft.AspNetCore.Mvc;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Controllers;

/// <summary>Makine filosu listesi (kategori gruplu) ve makine detay sayfası.</summary>
public class MachinesController : Controller
{
    private readonly IMachineService _machines;

    public MachinesController(IMachineService machines) => _machines = machines;

    public async Task<IActionResult> Index()
    {
        var culture = CultureContext.Current;
        var catalog = await _machines.GetCatalogAsync(culture);
        return View(catalog);
    }

    public async Task<IActionResult> Detail(string slug, string culture)
    {
        if (!CultureContext.IsSupported(culture)) culture = CultureContext.Current;

        var machine = await _machines.GetBySlugAsync(slug, culture);
        if (machine is null) return NotFound();

        ViewData["Title"] = machine.MetaTitle;
        ViewData["MetaDescription"] = machine.MetaDescription;
        return View(machine);
    }
}
