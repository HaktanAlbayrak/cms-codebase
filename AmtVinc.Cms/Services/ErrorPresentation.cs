namespace AmtVinc.Cms.Services;

/// <summary>
/// Hata ve durum (404/403/500...) sayfalarının metinlerini üretir. Kasıtlı olarak
/// <b>veritabanına ve lokalizasyon deposuna bağımlı değildir</b>: hata sayfası, tam da
/// patlamış olabilecek altyapıya (DB, cache, localization) güvenmemeli. Metinler kod
/// içinde, dile göre tutulur; böylece her şey çökse bile kullanıcı anlaşılır mesaj görür.
/// </summary>
public static class ErrorPresentation
{
    public record View(int StatusCode, string Title, string Message);

    private static readonly Dictionary<string, (string Title, string Message)> Generic = new()
    {
        ["tr"] = ("Bir sorun oluştu", "İsteğiniz işlenirken beklenmeyen bir sorun oluştu. Ekibimiz bilgilendirildi. Lütfen birazdan tekrar deneyin."),
        ["en"] = ("Something went wrong", "An unexpected problem occurred while processing your request. Our team has been notified. Please try again shortly."),
        ["ar"] = ("حدث خطأ ما", "حدثت مشكلة غير متوقعة أثناء معالجة طلبك. وقد تم إبلاغ فريقنا. يرجى المحاولة مرة أخرى بعد قليل."),
    };

    private static readonly Dictionary<string, (string Title, string Message)> NotFound = new()
    {
        ["tr"] = ("Sayfa bulunamadı", "Aradığınız sayfa taşınmış veya hiç var olmamış olabilir."),
        ["en"] = ("Page not found", "The page you are looking for may have moved or never existed."),
        ["ar"] = ("الصفحة غير موجودة", "ربما تم نقل الصفحة التي تبحث عنها أو أنها لم تكن موجودة أصلاً."),
    };

    private static readonly Dictionary<string, (string Title, string Message)> Forbidden = new()
    {
        ["tr"] = ("Erişim yok", "Bu sayfayı görüntüleme yetkiniz bulunmuyor."),
        ["en"] = ("Access denied", "You don't have permission to view this page."),
        ["ar"] = ("تم رفض الوصول", "ليس لديك إذن لعرض هذه الصفحة."),
    };

    private static readonly Dictionary<string, string> BackHome = new()
    {
        ["tr"] = "Ana sayfaya dön",
        ["en"] = "Back to home",
        ["ar"] = "العودة إلى الرئيسية",
    };

    public static View For(int statusCode, string culture, string? userMessage = null)
    {
        var lang = Normalize(culture);
        var (title, message) = statusCode switch
        {
            404 => Pick(NotFound, lang),
            403 => Pick(Forbidden, lang),
            _ => Pick(Generic, lang),
        };
        if (!string.IsNullOrWhiteSpace(userMessage))
            message = userMessage;
        return new View(statusCode, title, message);
    }

    public static string HomeLabel(string culture) => Pick(BackHome, Normalize(culture));

    /// <summary>Orijinal istek yolundan kullanıcının dilini çözer (ör. "/en/about" → "en").</summary>
    public static string CultureFromPath(string? originalPath)
    {
        if (!string.IsNullOrWhiteSpace(originalPath))
        {
            var seg = originalPath.TrimStart('/').Split('/', 2)[0];
            if (seg.Length == 2 && CultureContext.Supported.Contains(seg.ToLowerInvariant()))
                return seg.ToLowerInvariant();
        }
        return CultureContext.Default;
    }

    private const string Fallback = "tr";

    private static (string, string) Pick(Dictionary<string, (string, string)> map, string lang) =>
        map.TryGetValue(lang, out var v) ? v : map[Fallback];

    private static string Pick(Dictionary<string, string> map, string lang) =>
        map.TryGetValue(lang, out var v) ? v : map[Fallback];

    private static string Normalize(string? culture)
    {
        var c = (culture ?? CultureContext.Default).ToLowerInvariant();
        return Generic.ContainsKey(c) ? c : Fallback;
    }
}
