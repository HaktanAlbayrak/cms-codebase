namespace AmtVinc.Cms.Domain;

/// <summary>
/// Veritabanı destekli statik arayüz metni (IStringLocalizer kaynağı).
/// Örn: Key="nav.home", LanguageCode="tr", Value="Ana Sayfa".
/// Admin "Arayüz Metinleri" sayfasından düzenlenir.
/// </summary>
public class LocalizationResource : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
