namespace AmtVinc.Cms.Domain;

/// <summary>
/// Firmanın sunduğu hizmet (Vinç Kiralama, Platform Kiralama, Personel Yükseltici vb.).
/// Slug/ikon/görsel dilden bağımsız; başlık/özet/içerik çeviri tablosunda.
/// Ana sayfadaki hizmet grid'ini ve /tr/hizmetler listesini besler.
/// </summary>
public class Service : BaseEntity
{
    public string Slug { get; set; } = string.Empty;       // "vinc-kiralama"
    public string Icon { get; set; } = "construction";     // lucide ikon adı
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<ServiceTranslation> Translations { get; set; } = new List<ServiceTranslation>();
}

public class ServiceTranslation : BaseEntity
{
    public int ServiceId { get; set; }
    public Service? Service { get; set; }
    public string LanguageCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;     // kısa açıklama (kart + liste)
    public string Body { get; set; } = string.Empty;        // RichText (CKEditor) — detay
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}
