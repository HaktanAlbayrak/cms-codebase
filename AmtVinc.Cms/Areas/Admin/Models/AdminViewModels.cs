using AmtVinc.Cms.Domain;

namespace AmtVinc.Cms.Areas.Admin.Models;

/// <summary>Panel ana sayfası özet sayıları.</summary>
public class DashboardVm
{
    public int PageCount { get; init; }
    public int SlideCount { get; init; }
    public int LanguageCount { get; init; }
    public int MessageCount { get; init; }
    public int UnreadMessageCount { get; init; }
    public int UserCount { get; init; }
}

/// <summary>Kullanıcılar sayfası: liste + giriş yapan kullanıcının kimliği (kendini silmeyi/rol değiştirmeyi engellemek için).</summary>
public class UsersVm
{
    public List<AppUser> Users { get; init; } = new();
    public int CurrentUserId { get; init; }
}

/// <summary>Medya metadata düzenleme formu. Çeviri alanları dil-koduyla sözlüklenir.</summary>
public class MediaEditModel
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Dictionary<string, string> Title { get; set; } = new();
    public Dictionary<string, string> Alt { get; set; } = new();
    public Dictionary<string, string> Caption { get; set; } = new();
}

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

/// <summary>Hizmet düzenleme formu. Çeviri alanları dil-koduyla sözlüklenir.</summary>
public class ServiceEditModel
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Icon { get; set; } = "construction";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }

    public Dictionary<string, string> Title { get; set; } = new();
    public Dictionary<string, string> Summary { get; set; } = new();
    public Dictionary<string, string> Body { get; set; } = new();
    public Dictionary<string, string> MetaTitle { get; set; } = new();
    public Dictionary<string, string> MetaDescription { get; set; } = new();
}

/// <summary>Makine kategorisi düzenleme formu.</summary>
public class MachineCategoryEditModel
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Dictionary<string, string> Name { get; set; } = new();
}

/// <summary>Makine düzenleme formu. Teknik özellikler dilden bağımsız; ad/açıklama çeviri tablosunda.</summary>
public class MachineEditModel
{
    public int Id { get; set; }
    public int MachineCategoryId { get; set; }
    public string Slug { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public string? ImageUrl { get; set; }

    public string WorkingHeight { get; set; } = "";
    public string Capacity { get; set; } = "";
    public string Reach { get; set; } = "";
    public string Weight { get; set; } = "";

    public Dictionary<string, string> Name { get; set; } = new();
    public Dictionary<string, string> ShortDescription { get; set; } = new();
    public Dictionary<string, string> Description { get; set; } = new();
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
