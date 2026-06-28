using Microsoft.EntityFrameworkCore;
using Starter.Cms.Domain;

namespace Starter.Cms.Data;

/// <summary>
/// Uygulamanın EF Core veritabanı bağlamı. Scoped olarak kaydedilir; hiçbir yerde
/// elle <c>new</c> ile üretilmez (DIP).
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<LocalizationResource> LocalizationResources => Set<LocalizationResource>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemTranslation> MenuItemTranslations => Set<MenuItemTranslation>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<PageTranslation> PageTranslations => Set<PageTranslation>();
    public DbSet<Slide> Slides => Set<Slide>();
    public DbSet<SlideTranslation> SlideTranslations => Set<SlideTranslation>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<MediaAssetTranslation> MediaAssetTranslations => Set<MediaAssetTranslation>();
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ── Benzersizlik indeksleri ──
        b.Entity<Language>().HasIndex(x => x.Code).IsUnique();
        b.Entity<SiteSetting>().HasIndex(x => x.Key).IsUnique();
        b.Entity<Page>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<LocalizationResource>().HasIndex(x => new { x.Key, x.LanguageCode }).IsUnique();
        b.Entity<MediaAsset>().HasIndex(x => x.Url).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.Username).IsUnique();

        // Aynı dilde iki çeviri olmasın.
        b.Entity<PageTranslation>().HasIndex(x => new { x.PageId, x.LanguageCode }).IsUnique();
        b.Entity<SlideTranslation>().HasIndex(x => new { x.SlideId, x.LanguageCode }).IsUnique();
        b.Entity<MenuItemTranslation>().HasIndex(x => new { x.MenuItemId, x.LanguageCode }).IsUnique();
        b.Entity<MediaAssetTranslation>().HasIndex(x => new { x.MediaAssetId, x.LanguageCode }).IsUnique();

        // ── İlişkiler — çeviri tabloları ana kayıtla birlikte cascade silinir ──
        b.Entity<Page>().HasMany(x => x.Translations).WithOne(x => x.Page!)
            .HasForeignKey(x => x.PageId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Slide>().HasMany(x => x.Translations).WithOne(x => x.Slide!)
            .HasForeignKey(x => x.SlideId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<MenuItem>().HasMany(x => x.Translations).WithOne(x => x.MenuItem!)
            .HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<MediaAsset>().HasMany(x => x.Translations).WithOne(x => x.MediaAsset!)
            .HasForeignKey(x => x.MediaAssetId).OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>Kaydetmeden önce denetim alanlarını otomatik güncelle.</summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedDate = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
