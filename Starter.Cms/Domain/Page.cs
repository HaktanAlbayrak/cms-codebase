namespace Starter.Cms.Domain;

/// <summary>
/// Genel içerik sayfası (Hakkımızda, İletişim, Gizlilik, herhangi bir statik sayfa).
/// Slug/kapak/layout dilden bağımsız; başlık/gövde/SEO çeviri tablosunda.
/// Birden çok layout'a sahip olabilir (LayoutKey ile seçilir).
/// </summary>
public class Page : BaseEntity
{
    public string Slug { get; set; } = string.Empty;       // "about", "contact"
    public string CoverImageUrl { get; set; } = string.Empty;
    public string LayoutKey { get; set; } = "standard";    // standard | wide
    public int SortOrder { get; set; }

    public ICollection<PageTranslation> Translations { get; set; } = new List<PageTranslation>();
}

public class PageTranslation : BaseEntity
{
    public int PageId { get; set; }
    public Page? Page { get; set; }
    public string LanguageCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Lead { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;        // RichText (CKEditor)
    public string? MetaTitle { get; set; }                  // boşsa Title'a düşer
    public string? MetaDescription { get; set; }
}
