namespace AmtVinc.Cms.Services;

/// <summary>
/// Sayfa kompozisyonunu dayanıklı kılan yardımcılar. Ana sayfa gibi birden çok bağımsız
/// bölümden oluşan sayfalarda, kritik olmayan bir bölümün (slider, öne çıkanlar...)
/// patlaması tüm sayfayı düşürmemeli. <see cref="OrFallback{T}"/> hatayı loglar ve o bölüm
/// için güvenli bir varsayılan döner; böylece sayfa o bölüm boş şekilde ayakta kalır.
/// </summary>
public static class ResilienceExtensions
{
    public static async Task<T> OrFallback<T>(this Task<T> task, T fallback, ILogger logger, string section)
    {
        try
        {
            return await task;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "'{Section}' bölümü yüklenemedi; bölüm atlanıyor, sayfa ayakta tutuluyor.", section);
            return fallback;
        }
    }
}
