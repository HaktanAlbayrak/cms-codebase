namespace AmtVinc.Cms.Domain;

/// <summary>
/// Panel kullanıcı rolleri. <c>Admin</c> her şeyi yapar (kullanıcı/rol yönetimi dahil);
/// <c>ContentManager</c> yalnızca içerik modüllerini (sayfa, slider, medya, mesaj) yönetir.
/// Sistem ayarları (marka, mail, dil, arayüz metinleri) ve kullanıcı yönetimi admin'e özeldir.
/// </summary>
public enum UserRole
{
    ContentManager = 0,
    Admin = 1
}

/// <summary>
/// Rol adlarının tek kaynağı. <c>[Authorize(Roles = ...)]</c> ve claim'lerde
/// sihirli string yerine bu sabitler kullanılır (enum ile birebir aynı isimde olmalı).
/// </summary>
public static class Roles
{
    public const string Admin = nameof(UserRole.Admin);
    public const string ContentManager = nameof(UserRole.ContentManager);
}

/// <summary>
/// Panel kullanıcısı. Kimlik doğrulama artık appsettings'teki tek admin yerine bu tablodan
/// yapılır. Şifre düz metin tutulmaz; <see cref="PasswordHash"/> PBKDF2 (salt+iterasyon gömülü).
/// </summary>
public class AppUser : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.ContentManager;
    public DateTime? LastLoginAt { get; set; }
}
