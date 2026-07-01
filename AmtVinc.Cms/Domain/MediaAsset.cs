namespace AmtVinc.Cms.Domain;

/// <summary>Medya türü — uzantıdan tespit edilir, panelde filtreleme/ikon için kullanılır.</summary>
public enum MediaType
{
    Image = 0,
    Video = 1,
    Pdf = 2,
    Document = 3,
    Audio = 4,
    Other = 5
}

/// <summary>
/// Medya kütüphanesindeki tek bir dosya — artık dosya sisteminden değil, veritabanından
/// yönetilir (içerik gibi). Fiziksel dosya yine <c>wwwroot/uploads/{klasör}</c> altındadır;
/// bu kayıt onun yönetilebilir metadata'sını (tür, klasör, boyut, çok dilli alt/başlık/açıklama)
/// tutar. Görsel/video/PDF/belge tek bir kütüphanede toplanır ve <c>_MediaPicker</c> ile yeniden
/// kullanılır.
/// </summary>
public class MediaAsset : BaseEntity
{
    public string Url { get; set; } = string.Empty;          // /uploads/klasor/dosya.ext
    public string FileName { get; set; } = string.Empty;     // diskteki güvenli ad
    public string OriginalName { get; set; } = string.Empty; // yüklenen orijinal ad
    public MediaType Type { get; set; }
    public string Folder { get; set; } = "general";
    public string ContentType { get; set; } = string.Empty;  // MIME
    public long Size { get; set; }
    public int? Width { get; set; }                          // görseller için (best-effort)
    public int? Height { get; set; }
    public int SortOrder { get; set; }

    public ICollection<MediaAssetTranslation> Translations { get; set; } = new List<MediaAssetTranslation>();
}

/// <summary>Medyanın çok dilli metadata'sı (SEO/erişilebilirlik): başlık, alt metni, açıklama.</summary>
public class MediaAssetTranslation : BaseEntity
{
    public int MediaAssetId { get; set; }
    public MediaAsset? MediaAsset { get; set; }
    public string LanguageCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;   // dosya başlığı / görünen ad
    public string Alt { get; set; } = string.Empty;     // <img alt="..."> — SEO/erişilebilirlik
    public string Caption { get; set; } = string.Empty; // açıklama / kredi
}
