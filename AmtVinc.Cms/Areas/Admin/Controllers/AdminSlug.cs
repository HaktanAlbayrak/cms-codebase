namespace AmtVinc.Cms.Areas.Admin.Controllers;

/// <summary>Türkçe karakterleri sadeleştirip URL-dostu slug üretir (admin formları için ortak).</summary>
public static class AdminSlug
{
    public static string Make(string value)
    {
        var slug = new string(value.Trim().ToLowerInvariant()
            .Replace('ı', 'i').Replace('ğ', 'g').Replace('ü', 'u').Replace('ş', 's').Replace('ö', 'o').Replace('ç', 'c')
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        return string.Join('-', slug.Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}
