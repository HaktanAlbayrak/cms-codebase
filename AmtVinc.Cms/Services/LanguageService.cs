using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Data;

namespace AmtVinc.Cms.Services;

public record LanguageVm(string Code, string Name, bool IsRtl, bool IsDefault);

public interface ILanguageService
{
    Task<IReadOnlyList<LanguageVm>> GetActiveAsync();
}

public class LanguageService : ILanguageService
{
    private readonly ApplicationDbContext _db;
    private readonly ICachingService _cache;

    public LanguageService(ApplicationDbContext db, ICachingService cache)
    {
        _db = db;
        _cache = cache;
    }

    public Task<IReadOnlyList<LanguageVm>> GetActiveAsync() =>
        _cache.GetOrCreateAsync<IReadOnlyList<LanguageVm>>($"{IContentCache.Prefix}languages", async () =>
            (await _db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder)
                .AsNoTracking().ToListAsync())
            .Select(l => new LanguageVm(l.Code, l.Name, l.IsRtl, l.IsDefault)).ToList());
}
