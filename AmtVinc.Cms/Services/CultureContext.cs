using System.Globalization;

namespace AmtVinc.Cms.Services;

/// <summary>
/// Aktif istek kültürü + desteklenen diller için yardımcılar. Desteklenen diller
/// veritabanından beslenir (<see cref="Configure"/>), böylece yeni diller kod
/// değişmeden eklenebilir.
/// </summary>
public static class CultureContext
{
    private static volatile string[] _supported = { "tr" };
    private static volatile string _default = "tr";
    private static volatile IReadOnlySet<string> _rtl = new HashSet<string>();

    public static string Default => _default;
    public static IReadOnlyList<string> Supported => _supported;

    /// <summary>Startup'ta ve diller değiştiğinde DB'den çağrılır.</summary>
    public static void Configure(IEnumerable<(string Code, bool IsRtl, bool IsDefault)> languages)
    {
        var list = languages.ToList();
        if (list.Count == 0) return;
        _supported = list.Select(l => l.Code).ToArray();
        _default = list.FirstOrDefault(l => l.IsDefault).Code ?? list[0].Code;
        _rtl = list.Where(l => l.IsRtl).Select(l => l.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static string Current
    {
        get
        {
            var code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return IsSupported(code) ? code : _default;
        }
    }

    public static bool IsSupported(string? code) =>
        !string.IsNullOrEmpty(code) && _supported.Contains(code, StringComparer.OrdinalIgnoreCase);

    public static bool IsRtl(string code) => _rtl.Contains(code);

    /// <summary>Verilen yolu aktif kültür önekiyle döndürür: "/about" → "/tr/about".</summary>
    public static string LocalizedPath(string path)
    {
        var c = Current;
        if (string.IsNullOrEmpty(path) || path == "/") return "/" + c;
        return "/" + c + (path.StartsWith('/') ? path : "/" + path);
    }

    /// <summary>Geçerli isteğin URL'ini hedef kültüre çevirir (dil değiştirici için).</summary>
    public static string SwitchCulture(HttpContext http, string target)
    {
        var path = http.Request.Path.Value ?? "/";
        var qs = http.Request.QueryString.Value;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (segments.Count > 0 && IsSupported(segments[0])) segments[0] = target;
        else segments.Insert(0, target);
        return "/" + string.Join('/', segments) + qs;
    }

    /// <summary>Verilen çeviri listesinden aktif dile uygun olanı (yoksa varsayılanı) seçer.</summary>
    public static T? Pick<T>(IEnumerable<T> translations, Func<T, string> langSelector, string culture)
    {
        var list = translations.ToList();
        return list.FirstOrDefault(t => string.Equals(langSelector(t), culture, StringComparison.OrdinalIgnoreCase))
            ?? list.FirstOrDefault(t => string.Equals(langSelector(t), _default, StringComparison.OrdinalIgnoreCase))
            ?? list.FirstOrDefault();
    }
}
