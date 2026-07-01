namespace AmtVinc.Cms.Domain;

/// <summary>
/// Makine filosu kategorisi (Manlift, Makaslı Platform, Eklemli Platform, Sepetli Vinç...).
/// Slug/sıra dilden bağımsız; ad çeviri tablosunda. Bir kategori birden çok makine içerir.
/// </summary>
public class MachineCategory : BaseEntity
{
    public string Slug { get; set; } = string.Empty;       // "makasli-platform"
    public int SortOrder { get; set; }

    public ICollection<MachineCategoryTranslation> Translations { get; set; } = new List<MachineCategoryTranslation>();
    public ICollection<Machine> Machines { get; set; } = new List<Machine>();
}

public class MachineCategoryTranslation : BaseEntity
{
    public int MachineCategoryId { get; set; }
    public MachineCategory? Category { get; set; }
    public string LanguageCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Filodaki tek bir makine/ekipman (ör. "16 m Makaslı Platform").
/// Teknik özellikler (çalışma yüksekliği, kapasite, ağırlık) dilden bağımsız metinlerdir
/// (sayı + birim, ör. "16 m"); ad/açıklama çeviri tablosunda. IsFeatured ana sayfada öne çıkarır.
/// </summary>
public class Machine : BaseEntity
{
    public int MachineCategoryId { get; set; }
    public MachineCategory? Category { get; set; }

    public string Slug { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsFeatured { get; set; }                   // ana sayfa "öne çıkan makineler"

    // ── Teknik özellikler (dilden bağımsız; sayı + birim metni) ──
    public string WorkingHeight { get; set; } = string.Empty;   // çalışma yüksekliği, ör. "16 m"
    public string Capacity { get; set; } = string.Empty;        // sepet/platform kapasitesi, ör. "230 kg"
    public string Reach { get; set; } = string.Empty;           // yatay erişim / yan ulaşım, ör. "9 m"
    public string Weight { get; set; } = string.Empty;          // makine ağırlığı, ör. "2.900 kg"

    public ICollection<MachineTranslation> Translations { get; set; } = new List<MachineTranslation>();
}

public class MachineTranslation : BaseEntity
{
    public int MachineId { get; set; }
    public Machine? Machine { get; set; }
    public string LanguageCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;     // RichText — detay sayfası
}
