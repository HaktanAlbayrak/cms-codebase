namespace AmtVinc.Cms.Localization;

/// <summary>Veritabanı destekli statik metin deposu (cache'li).</summary>
public interface ILocalizationStore
{
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(string culture);
    string Get(string culture, string key);
    void Invalidate();
}
