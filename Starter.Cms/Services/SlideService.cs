using Microsoft.EntityFrameworkCore;
using Starter.Cms.Data;
using Starter.Cms.ViewModels;

namespace Starter.Cms.Services;

public interface ISlideService
{
    Task<IReadOnlyList<SlideVm>> GetSlidesAsync(string culture);
}

public class SlideService : ISlideService
{
    private readonly ApplicationDbContext _db;
    private readonly ICachingService _cache;

    public SlideService(ApplicationDbContext db, ICachingService cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<SlideVm>> GetSlidesAsync(string culture) =>
        _cache.GetOrCreateAsync<IReadOnlyList<SlideVm>>($"{IContentCache.Prefix}slides:{culture}", async () =>
        {
            var slides = await _db.Slides
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .Include(s => s.Translations)
                .AsNoTracking().ToListAsync();

            return slides.Select(s =>
            {
                var t = CultureContext.Pick(s.Translations, x => x.LanguageCode, culture);
                return new SlideVm(
                    s.ImageDesktop,
                    string.IsNullOrWhiteSpace(s.ImageMobile) ? s.ImageDesktop : s.ImageMobile,
                    t?.Kicker ?? "", t?.Title ?? "", t?.Subtitle ?? "",
                    t?.CtaText ?? "", t?.CtaUrl ?? "#");
            }).ToList();
        });
}
