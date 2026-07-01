namespace AmtVinc.Cms.Domain;

/// <summary>
/// Anahtar-değer site ayarı (telefon, e-posta, sosyal medya, WhatsApp no, marka,
/// tema rengi, SMTP, SEO...). Çevrilebilir metinler için değil; sabit yapılandırma
/// değerleri içindir. Statik arayüz metinleri <see cref="LocalizationResource"/>'tadır.
/// </summary>
public class SiteSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Group { get; set; } = "general";  // gruplama: contact, social, branding, theme, smtp, seo...
}
