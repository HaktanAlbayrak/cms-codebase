namespace Starter.Cms.Domain;

/// <summary>
/// Header/footer menü öğesi. Etiket dile göre değişir (çeviri tablosu); URL ve sıra
/// dilden bağımsızdır. Menü tamamen DB'den beslenir (sıfır statik link).
/// </summary>
public class MenuItem : BaseEntity
{
    public string Url { get; set; } = string.Empty;   // "/", "/about", "/contact"
    public int SortOrder { get; set; }

    public ICollection<MenuItemTranslation> Translations { get; set; } = new List<MenuItemTranslation>();
}

public class MenuItemTranslation : BaseEntity
{
    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
