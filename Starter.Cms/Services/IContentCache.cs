namespace Starter.Cms.Services;

/// <summary>Frontend içerik cache'ini topluca temizlemek için ortak sözleşme.</summary>
public interface IContentCache
{
    const string Prefix = "content:";
    void InvalidateAll();
}

public class ContentCache : IContentCache
{
    private readonly ICachingService _cache;
    public ContentCache(ICachingService cache) => _cache = cache;
    public void InvalidateAll() => _cache.RemoveByPrefix(IContentCache.Prefix);
}
