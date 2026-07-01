namespace AmtVinc.Cms.Services;

/// <summary>
/// Yeni şifreler için minimum güç politikası. Tek kaynak: kullanıcı oluşturma, şifre
/// sıfırlama ve profilden şifre değiştirme aynı kuralları paylaşır.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public const string RuleText =
        "Şifre en az 8 karakter olmalı ve en az bir harf ile bir rakam içermelidir.";

    /// <summary>Geçerliyse <c>null</c>, değilse Türkçe hata mesajı döner.</summary>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
            return RuleText;
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return RuleText;
        return null;
    }
}
