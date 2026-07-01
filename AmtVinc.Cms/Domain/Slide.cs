namespace AmtVinc.Cms.Domain;

/// <summary>Ana sayfa hero slider slaytı. Çoklu dil metinleri çeviri tablosunda.</summary>
public class Slide : BaseEntity
{
    public string ImageDesktop { get; set; } = string.Empty;
    public string ImageMobile { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public ICollection<SlideTranslation> Translations { get; set; } = new List<SlideTranslation>();
}

public class SlideTranslation : BaseEntity
{
    public int SlideId { get; set; }
    public Slide? Slide { get; set; }
    public string LanguageCode { get; set; } = string.Empty;

    public string Kicker { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string CtaText { get; set; } = string.Empty;
    public string CtaUrl { get; set; } = string.Empty;
}
