using Microsoft.AspNetCore.Mvc;
using Starter.Cms.Services;
using Starter.Cms.ViewModels;

namespace Starter.Cms.ViewComponents;

/// <summary>DB'den beslenen header: logo (branding), menü ve dil değiştirici.</summary>
public class HeaderViewComponent : ViewComponent
{
    private readonly INavigationService _nav;
    private readonly IBrandingService _branding;
    private readonly ILanguageService _languages;

    public HeaderViewComponent(INavigationService nav, IBrandingService branding, ILanguageService languages)
    {
        _nav = nav;
        _branding = branding;
        _languages = languages;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var culture = CultureContext.Current;
        var vm = new HeaderFooterVm
        {
            Branding = await _branding.GetAsync(),
            Menu = await _nav.GetMenuAsync(culture),
            Languages = await _languages.GetActiveAsync(),
            Culture = culture
        };
        return View(vm);
    }
}

/// <summary>Header ve footer için ortak görünüm modeli.</summary>
public class HeaderFooterVm
{
    public required BrandingInfo Branding { get; init; }
    public required IReadOnlyList<MenuItemVm> Menu { get; init; }
    public required IReadOnlyList<LanguageVm> Languages { get; init; }
    public required string Culture { get; init; }
}
