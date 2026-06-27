namespace Starter.Cms.Domain;

/// <summary>
/// Sistemde desteklenen dil. Çoklu dil altyapısının temelidir (N-dil).
/// Yeni dil eklemek kod değişikliği gerektirmez — panelden eklenir.
/// </summary>
public class Language : BaseEntity
{
    public string Code { get; set; } = string.Empty;   // "tr", "en"
    public string Name { get; set; } = string.Empty;   // "Türkçe", "English"
    public bool IsDefault { get; set; }
    public bool IsRtl { get; set; }   // Arapça gibi sağdan-sola diller için
    public int SortOrder { get; set; }
}
