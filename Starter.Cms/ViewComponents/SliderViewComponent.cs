using Microsoft.AspNetCore.Mvc;
using Starter.Cms.Services;
using Starter.Cms.ViewModels;

namespace Starter.Cms.ViewComponents;

/// <summary>Ana sayfa hero slider'ı (DB beslemeli, dile göre).</summary>
public class SliderViewComponent : ViewComponent
{
    private readonly ISlideService _slides;

    public SliderViewComponent(ISlideService slides) => _slides = slides;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var slides = await _slides.GetSlidesAsync(CultureContext.Current);
        return View(slides);
    }
}
