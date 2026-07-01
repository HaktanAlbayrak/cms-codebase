using Microsoft.AspNetCore.Mvc;
using AmtVinc.Cms.Services;
using AmtVinc.Cms.ViewModels;

namespace AmtVinc.Cms.ViewComponents;

/// <summary>DB'den beslenen footer: marka, menü ve sosyal linkler.</summary>
public class FooterViewComponent : ViewComponent
{
    private readonly INavigationService _nav;
    private readonly IBrandingService _branding;
    private readonly ILanguageService _languages;

    public FooterViewComponent(INavigationService nav, IBrandingService branding, ILanguageService languages)
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
