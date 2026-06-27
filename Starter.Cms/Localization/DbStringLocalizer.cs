using System.Globalization;
using Microsoft.Extensions.Localization;

namespace Starter.Cms.Localization;

/// <summary>
/// Veritabanı destekli IStringLocalizer. Razor'da <c>@Localizer["nav.home"]</c>
/// çağrıldığında aktif kültüre göre DB metnini döndürür.
/// </summary>
public class DbStringLocalizer : IStringLocalizer
{
    private readonly ILocalizationStore _store;
    public DbStringLocalizer(ILocalizationStore store) => _store = store;

    private static string CurrentCulture => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    public LocalizedString this[string name]
    {
        get
        {
            var value = _store.Get(CurrentCulture, name);
            return new LocalizedString(name, value, resourceNotFound: value == name);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = _store.Get(CurrentCulture, name);
            var value = string.Format(CultureInfo.CurrentCulture, format, arguments);
            return new LocalizedString(name, value, resourceNotFound: format == name);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var all = _store.GetAllAsync(CurrentCulture).GetAwaiter().GetResult();
        return all.Select(kv => new LocalizedString(kv.Key, kv.Value, false));
    }
}

/// <summary>Tüm tipler için aynı DB localizer'ı döndüren fabrika.</summary>
public class DbStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly ILocalizationStore _store;
    public DbStringLocalizerFactory(ILocalizationStore store) => _store = store;

    public IStringLocalizer Create(Type resourceSource) => new DbStringLocalizer(_store);
    public IStringLocalizer Create(string baseName, string location) => new DbStringLocalizer(_store);
}
