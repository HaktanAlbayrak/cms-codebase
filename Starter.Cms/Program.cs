using Microsoft.AspNetCore.Localization.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Starter.Cms.Data;
using Starter.Cms.Localization;
using Starter.Cms.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Yükleme boyutu sınırlarını gevşet (büyük slider/medya görselleri için) ──
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = null);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = long.MaxValue;
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartHeadersLengthLimit = int.MaxValue;
});

// ── Veritabanı (SQLite — sıfır kurulumla çalışır; MSSQL'e tek satırda geçilir) ──
var connectionString = builder.Configuration.GetConnectionString("CmsDb")
    ?? "Data Source=starter-cms.db";
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connectionString));

// ── Altyapı servisleri ──
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICachingService, CachingService>();
builder.Services.AddSingleton<ILocalizationStore, LocalizationStore>();
builder.Services.AddScoped<IContentCache, ContentCache>();

// ── İş servisleri (DIP — interface'e bağımlılık) ──
builder.Services.AddScoped<ISiteSettingService, SiteSettingService>();
builder.Services.AddScoped<IBrandingService, BrandingService>();
builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<ISlideService, SlideService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<IAdminAuthService, AdminAuthService>();

// ── SMTP / e-posta (ayarlar DB'den · SiteSettings smtp.* okunur) ──
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// ── Veritabanı destekli localization ──
builder.Services.AddLocalization();
builder.Services.AddSingleton<IStringLocalizerFactory, DbStringLocalizerFactory>();

// ── Çoklu dil (route tabanlı kültür) ──
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = CultureContext.Supported.ToArray();
    options.SetDefaultCulture(CultureContext.Default)
           .AddSupportedCultures(cultures)
           .AddSupportedUICultures(cultures);
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new RouteDataRequestCultureProvider
    {
        RouteDataStringKey = "culture",
        UIRouteDataStringKey = "culture"
    });
});

// ── Kimlik doğrulama (admin) ──
builder.Services.AddAuthentication(IAdminAuthService.Scheme)
    .AddCookie(IAdminAuthService.Scheme, o =>
    {
        o.LoginPath = "/admin/account/login";
        o.AccessDeniedPath = "/admin/account/login";
        o.Cookie.Name = "StarterCmsAdmin";
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews(options =>
{
    // Nullable referans tipleri için otomatik (İngilizce) "field is required" doğrulamasını
    // kapat; yalnızca bizim açıkça eklediğimiz Türkçe mesajlar gösterilsin.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

var app = builder.Build();

// ── Veritabanı oluştur + seed, ardından dilleri DB'den yükle ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db);

    var languages = db.Languages.Where(l => l.IsActive).OrderBy(l => l.SortOrder)
        .Select(l => new { l.Code, l.IsRtl, l.IsDefault }).ToList();
    CultureContext.Configure(languages.Select(l => (l.Code, l.IsRtl, l.IsDefault)));
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler($"/{CultureContext.Default}/Home/Error");
    app.UseHsts();
}

// Boş gövdeli 4xx/5xx yanıtlarını dostane, lokalize sayfaya yeniden çalıştır.
app.UseStatusCodePagesWithReExecute($"/{CultureContext.Default}/Home/Status", "?code={0}");

app.UseStaticFiles();
app.UseRouting();

// Desteklenen kültürleri DB'deki dillerden doldur (yeni diller eklenebilir).
var locOptions = app.Services
    .GetRequiredService<Microsoft.Extensions.Options.IOptions<RequestLocalizationOptions>>().Value;
foreach (var code in CultureContext.Supported)
{
    var ci = System.Globalization.CultureInfo.GetCultureInfo(code);
    if (locOptions.SupportedCultures!.All(c => c.Name != ci.Name)) locOptions.SupportedCultures!.Add(ci);
    if (locOptions.SupportedUICultures!.All(c => c.Name != ci.Name)) locOptions.SupportedUICultures!.Add(ci);
}
locOptions.SetDefaultCulture(CultureContext.Default);
app.UseRequestLocalization(locOptions);

app.UseAuthentication();
app.UseAuthorization();

// ── Route'lar ──
// Admin alanı (kültür öneki yok).
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Kültür kökü → ana sayfa (/tr).
app.MapControllerRoute(
    name: "home",
    pattern: "{culture:regex(^[a-zA-Z]{{2}}$)}",
    defaults: new { controller = "Home", action = "Index" });

// İletişim (form içerir).
app.MapControllerRoute(
    name: "contact",
    pattern: "{culture:regex(^[a-zA-Z]{{2}}$)}/contact",
    defaults: new { controller = "Contact", action = "Index" });

// Genel içerik sayfaları slug ile (/tr/about, /tr/gizlilik ...). Yeni sayfa = kod değişmeden.
app.MapControllerRoute(
    name: "page",
    pattern: "{culture:regex(^[a-zA-Z]{{2}}$)}/{slug}",
    defaults: new { controller = "Pages", action = "Detail" });

// Kök → varsayılan dile yönlendir.
app.MapGet("/", context =>
{
    context.Response.Redirect($"/{CultureContext.Default}");
    return Task.CompletedTask;
});

app.Run();
