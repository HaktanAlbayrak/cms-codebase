using Starter.Cms.Services;

namespace Starter.Cms.Areas.Admin.Controllers;

/// <summary>
/// Admin panelinde kullanıcıya gösterilen tüm metinlerin tek kaynağı. Panel tek dilli
/// (Türkçe) olduğundan resx yerine sabitler yeterlidir.
/// </summary>
public static class AdminMessages
{
    public const string UnexpectedError = "İşlem sırasında beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.";

    public const string PageSaved = "Sayfa kaydedildi.";
    public const string PageDeleted = "Sayfa silindi.";

    public const string SlideSaved = "Slayt kaydedildi.";
    public const string SlideDeleted = "Slayt silindi.";
    public const string SlideImageRequired = "Masaüstü görseli zorunludur (dosya yükleyin veya medyadan seçin).";

    public const string BrandingSaved = "Marka & tema ayarları kaydedildi.";
    public const string MailSettingsSaved = "E-posta ayarları kaydedildi.";

    public const string MediaUploaded = "Dosya yüklendi.";
    public const string MediaDeleted = "Dosya silindi.";
    public const string MediaSaved = "Medya bilgileri kaydedildi.";

    public const string ContactMessageDeleted = "Mesaj silindi.";

    public const string LanguageSaved = "Dil kaydedildi.";
    public const string LanguageDeleted = "Dil silindi.";
    public const string LanguageAddedSeeded = "Dil eklendi. Arayüz metinleri varsayılan dilden kopyalandı; 'Arayüz Metinleri' sayfasından çevirebilirsiniz.";
    public const string DefaultLanguageCannotBeDeleted = "Varsayılan dil silinemez.";
    public const string InvalidCultureCode = "Geçersiz kültür kodu (örn: tr, en, ar).";

    public const string TranslationsSaved = "Çeviriler kaydedildi.";
    public const string InvalidCredentials = "Kullanıcı adı veya şifre hatalı.";

    public static string MailTestSent(string? email) =>
        $"Test e-postası gönderildi{(string.IsNullOrWhiteSpace(email) ? "" : $": {email}")}. " +
        "Gelen kutusunu (ve spam klasörünü) kontrol edin.";

    public static string MailTestFailed(string detail) => "Test e-postası gönderilemedi: " + detail;
}
