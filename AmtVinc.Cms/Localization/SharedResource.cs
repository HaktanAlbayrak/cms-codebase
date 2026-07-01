namespace AmtVinc.Cms.Localization;

/// <summary>
/// IStringLocalizer için işaretçi tip. Razor'da <c>@inject IStringLocalizer&lt;SharedResource&gt; Localizer</c>
/// ile enjekte edilir; fabrikamız tipi yok sayıp DB destekli localizer döndürür.
/// </summary>
public sealed class SharedResource { }
