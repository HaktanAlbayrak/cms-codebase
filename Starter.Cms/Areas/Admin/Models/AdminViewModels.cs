namespace Starter.Cms.Areas.Admin.Models;

/// <summary>Panel ana sayfası özet sayıları.</summary>
public class DashboardVm
{
    public int PageCount { get; init; }
    public int SlideCount { get; init; }
    public int LanguageCount { get; init; }
    public int MessageCount { get; init; }
    public int UnreadMessageCount { get; init; }
}

/// <summary>Medya kütüphanesindeki tek bir dosya.</summary>
public record MediaItem(string Url, string Name, string Folder, long Size, DateTime Modified);

/// <summary>Sayfa düzenleme formu. Çeviri alanları dil-koduyla sözlüklenir (Title["tr"]).</summary>
public class PageEditModel
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string LayoutKey { get; set; } = "standard";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CoverImageUrl { get; set; }

    public Dictionary<string, string> Title { get; set; } = new();
    public Dictionary<string, string> Lead { get; set; } = new();
    public Dictionary<string, string> Body { get; set; } = new();
    public Dictionary<string, string> MetaTitle { get; set; } = new();
    public Dictionary<string, string> MetaDescription { get; set; } = new();
}

/// <summary>Slayt düzenleme formu. Çeviri alanları dil-koduyla sözlüklenir.</summary>
public class SlideEditModel
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageDesktop { get; set; }
    public string? ImageMobile { get; set; }

    public Dictionary<string, string> Kicker { get; set; } = new();
    public Dictionary<string, string> Title { get; set; } = new();
    public Dictionary<string, string> Subtitle { get; set; } = new();
    public Dictionary<string, string> CtaText { get; set; } = new();
    public Dictionary<string, string> CtaUrl { get; set; } = new();
}
