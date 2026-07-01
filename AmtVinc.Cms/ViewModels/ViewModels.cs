using System.ComponentModel.DataAnnotations;

namespace AmtVinc.Cms.ViewModels;

/// <summary>Frontend hero slider slaytı.</summary>
public record SlideVm(
    string ImageDesktop,
    string ImageMobile,
    string Kicker,
    string Title,
    string Subtitle,
    string CtaText,
    string CtaUrl);

/// <summary>Header/footer menü öğesi.</summary>
public record MenuItemVm(string Label, string Url);

/// <summary>Genel içerik sayfası görünümü (Hakkımızda vb.).</summary>
public record PageVm(
    string Slug,
    string LayoutKey,
    string CoverImageUrl,
    string Title,
    string Lead,
    string Body,
    string MetaTitle,
    string MetaDescription);

/// <summary>Hizmet (kart, liste ve detay için ortak görünüm).</summary>
public record ServiceVm(
    string Slug,
    string Icon,
    string ImageUrl,
    string Title,
    string Summary,
    string Body,
    string MetaTitle,
    string MetaDescription);

/// <summary>Filo makinesi (kart ve detay için ortak görünüm).</summary>
public record MachineVm(
    string Slug,
    string ImageUrl,
    string Name,
    string ShortDescription,
    string Description,
    string CategorySlug,
    string CategoryName,
    string WorkingHeight,
    string Capacity,
    string Reach,
    string Weight,
    string MetaTitle,
    string MetaDescription);

/// <summary>Bir makine kategorisi ve içindeki makineler (filo listesi gruplaması).</summary>
public record MachineCategoryVm(
    string Slug,
    string Name,
    IReadOnlyList<MachineVm> Machines);

/// <summary>İletişim formu (model binding + doğrulama).</summary>
public class ContactFormModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [StringLength(160)]
    public string Email { get; set; } = string.Empty;

    [StringLength(40)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Konu zorunludur.")]
    [StringLength(160)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mesaj zorunludur.")]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;
}
