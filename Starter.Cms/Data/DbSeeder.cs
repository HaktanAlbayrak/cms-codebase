using Microsoft.EntityFrameworkCore;
using Starter.Cms.Domain;
using Starter.Cms.Services;

namespace Starter.Cms.Data;

/// <summary>
/// İlk açılışta base içeriğini (TR/EN) tohumlar. Her bölüm idempotenttir:
/// ilgili tablo/anahtar boşsa ekler, mevcut veriye dokunmaz. Böylece yeni projeye
/// kopyalandığında çalışır hale gelir; sonradan eklenen ayar anahtarları da gelir.
/// </summary>
public static class DbSeeder
{
    // Demo görselleri (Unsplash). Yeni projede medya kütüphanesinden kendi görsellerinle değiştir.
    private const string Hero1 = "https://images.unsplash.com/photo-1497366216548-37526070297c?q=80&w=1600&auto=format&fit=crop";
    private const string Hero2 = "https://images.unsplash.com/photo-1521737711867-e3b97375f902?q=80&w=1600&auto=format&fit=crop";

    public static async Task SeedAsync(ApplicationDbContext db, IConfiguration? config = null)
    {
        await SeedLanguagesAsync(db);
        await SeedLocalizationAsync(db);
        await SeedSettingsAsync(db);
        await SeedMenuAsync(db);
        await SeedSlidesAsync(db);
        await SeedPagesAsync(db);
        await SeedUsersAsync(db, config);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// İlk açılışta varsayılan admin kullanıcısını oluşturur (appsettings.json'daki
    /// <c>Admin:Username</c>/<c>Admin:Password</c> tohum olarak kullanılır). Tabloda kullanıcı
    /// varsa dokunmaz — şifreler artık panelden yönetilir, appsettings yalnızca ilk tohumdur.
    /// </summary>
    private static async Task SeedUsersAsync(ApplicationDbContext db, IConfiguration? config)
    {
        if (await db.Users.AnyAsync()) return;
        var username = config?["Admin:Username"] ?? "admin";
        var password = config?["Admin:Password"] ?? "admin123";
        db.Users.Add(new AppUser
        {
            Username = username.Trim(),
            FullName = "Yönetici",
            Email = "",
            Role = UserRole.Admin,
            PasswordHash = PasswordHasher.Hash(password)
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedLanguagesAsync(ApplicationDbContext db)
    {
        if (await db.Languages.AnyAsync()) return;
        db.Languages.AddRange(
            new Language { Code = "tr", Name = "Türkçe", IsDefault = true, SortOrder = 1 },
            new Language { Code = "en", Name = "English", IsDefault = false, SortOrder = 2 });
        await db.SaveChangesAsync();
    }

    private static async Task SeedMenuAsync(ApplicationDbContext db)
    {
        if (await db.MenuItems.AnyAsync()) return;
        void Add(string url, int order, string tr, string en) =>
            db.MenuItems.Add(new MenuItem
            {
                Url = url,
                SortOrder = order,
                Translations =
                {
                    new MenuItemTranslation { LanguageCode = "tr", Label = tr },
                    new MenuItemTranslation { LanguageCode = "en", Label = en }
                }
            });
        Add("/", 1, "Ana Sayfa", "Home");
        Add("/about", 2, "Hakkımızda", "About");
        Add("/contact", 3, "İletişim", "Contact");
        await db.SaveChangesAsync();
    }

    private static async Task SeedSettingsAsync(ApplicationDbContext db)
    {
        // Anahtar-bazlı idempotent upsert: tablo dolu olsa bile EKSİK anahtarlar eklenir.
        var present = new HashSet<string>(await db.SiteSettings.Select(s => s.Key).ToListAsync());
        void Add(string key, string value, string group)
        {
            if (present.Contains(key)) return;
            db.SiteSettings.Add(new SiteSetting { Key = key, Value = value, Group = group });
            present.Add(key);
        }

        // İletişim & sosyal
        Add("contact.phone", "+90 (212) 000 00 00", "contact");
        Add("contact.email", "info@example.com", "contact");
        Add("whatsapp.number", "905000000000", "general");
        Add("social.facebook", "", "social");
        Add("social.instagram", "", "social");
        Add("social.linkedin", "", "social");
        Add("social.youtube", "", "social");

        // Marka. Logo header/footer'da zorunludur; varsayılan logo/favicon depoda hazır gelir
        // (/img/logo.svg · /img/logo-light.svg · /favicon.svg), panelden değiştirilebilir.
        Add("branding.companyName", "Starter", "branding");
        Add("branding.logoUrl", "/img/logo.svg", "branding");
        Add("branding.logoLightUrl", "/img/logo-light.svg", "branding");
        Add("branding.faviconUrl", "/favicon.svg", "branding");
        Add("branding.address", "Örnek Mah. Örnek Cad. No:1, İstanbul", "branding");
        Add("branding.workingHours", "Pzt - Cuma: 09:00 - 18:00", "branding");

        // Tema renkleri (hex; layout RGB kanala çevirir)
        Add("theme.primary", "#2563eb", "theme");
        Add("theme.primaryDark", "#1e40af", "theme");
        Add("theme.ink", "#0f172a", "theme");

        // SEO
        Add("seo.ogImageUrl", "", "seo");

        // E-posta (SMTP) — appsettings.json'a gömülmez; panelden yönetilir.
        Add("smtp.enabled", "false", "smtp");
        Add("smtp.host", "smtp.gmail.com", "smtp");
        Add("smtp.port", "587", "smtp");
        Add("smtp.ssl", "true", "smtp");
        Add("smtp.user", "", "smtp");
        Add("smtp.password", "", "smtp");
        Add("smtp.fromEmail", "", "smtp");
        Add("smtp.fromName", "Starter Web", "smtp");
        Add("smtp.toEmail", "info@example.com", "smtp");

        // İletişim formu bildirim e-postası (panelden düzenlenebilir; {{...}} yer tutucuları)
        Add("mail.contactSubject", "[İletişim] {{subject}}", "mail");
        Add("mail.contactTemplate", DefaultContactTemplate, "mail");

        await db.SaveChangesAsync();
    }

    private const string DefaultContactTemplate = """
        <div style="font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;border:1px solid #eee;border-radius:8px;overflow:hidden">
          <div style="background:#0f172a;color:#fff;padding:20px 24px">
            <h2 style="margin:0;font-size:18px">Yeni İletişim Mesajı</h2>
          </div>
          <div style="padding:24px;color:#333;font-size:14px;line-height:1.6">
            <p><strong>Ad Soyad:</strong> {{name}}</p>
            <p><strong>E-posta:</strong> {{email}}</p>
            <p><strong>Telefon:</strong> {{phone}}</p>
            <p><strong>Konu:</strong> {{subject}}</p>
            <hr style="border:none;border-top:1px solid #eee;margin:16px 0" />
            <p>{{message}}</p>
            <p style="color:#888;font-size:12px">Gönderim: {{date}}</p>
          </div>
        </div>
        """;

    private static async Task SeedSlidesAsync(ApplicationDbContext db)
    {
        if (await db.Slides.AnyAsync()) return;
        var data = new (string Img, string KTr, string TTr, string STr, string CTr,
                        string KEn, string TEn, string SEn, string CEn)[]
        {
            (Hero1, "HOŞ GELDİNİZ", "Modern, Çok Dilli CMS Altyapısı",
             "Tüm içerik admin panelden yönetilir — sıfır statik metin.", "İletişime Geç",
             "WELCOME", "Modern, Multilingual CMS Foundation",
             "All content is managed from the admin panel — zero hard-coded text.", "Get in Touch"),
            (Hero2, "HAZIR BAŞLANGIÇ", "Yeni Projeye Saatler Değil Dakikalar İçinde",
             "Mail servisi, medya kütüphanesi ve çok dillilik kutudan çıkar çıkmaz hazır.", "Hakkımızda",
             "READY START", "Start New Projects in Minutes, Not Hours",
             "Mail service, media library and multilingual support work out of the box.", "About Us"),
        };
        int order = 1;
        foreach (var s in data)
        {
            db.Slides.Add(new Slide
            {
                ImageDesktop = s.Img,
                ImageMobile = s.Img,
                SortOrder = order++,
                Translations =
                {
                    new SlideTranslation { LanguageCode = "tr", Kicker = s.KTr, Title = s.TTr, Subtitle = s.STr, CtaText = s.CTr, CtaUrl = "/contact" },
                    new SlideTranslation { LanguageCode = "en", Kicker = s.KEn, Title = s.TEn, Subtitle = s.SEn, CtaText = s.CEn, CtaUrl = "/contact" }
                }
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedPagesAsync(ApplicationDbContext db)
    {
        if (await db.Pages.AnyAsync()) return;

        db.Pages.Add(new Page
        {
            Slug = "about",
            LayoutKey = "standard",
            SortOrder = 1,
            Translations =
            {
                new PageTranslation
                {
                    LanguageCode = "tr",
                    Title = "Hakkımızda",
                    Lead = "Bu sayfa tamamen admin panelden düzenlenebilir.",
                    Body = "<p>Bu örnek bir <strong>Hakkımızda</strong> sayfasıdır. İçeriği admin paneldeki <em>Sayfalar</em> bölümünden, dil sekmeleriyle düzenleyebilirsiniz. Görseller medya kütüphanesinden seçilir.</p>",
                    MetaTitle = "Hakkımızda",
                    MetaDescription = "Firmamız hakkında bilgi."
                },
                new PageTranslation
                {
                    LanguageCode = "en",
                    Title = "About Us",
                    Lead = "This page is fully editable from the admin panel.",
                    Body = "<p>This is a sample <strong>About</strong> page. You can edit its content from the <em>Pages</em> section in the admin panel using language tabs. Images are picked from the media library.</p>",
                    MetaTitle = "About Us",
                    MetaDescription = "Information about our company."
                }
            }
        });

        db.Pages.Add(new Page
        {
            Slug = "contact",
            LayoutKey = "standard",
            SortOrder = 2,
            Translations =
            {
                new PageTranslation { LanguageCode = "tr", Title = "İletişim", Lead = "Bize ulaşın.", Body = "" },
                new PageTranslation { LanguageCode = "en", Title = "Contact", Lead = "Get in touch.", Body = "" }
            }
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedLocalizationAsync(ApplicationDbContext db)
    {
        // (key, tr, en) — statik arayüz metinleri. Yeni anahtarlar idempotent eklenir.
        var rows = new (string Key, string Tr, string En)[]
        {
            ("nav.home", "Ana Sayfa", "Home"),
            ("nav.about", "Hakkımızda", "About"),
            ("nav.contact", "İletişim", "Contact"),
            ("nav.cta", "İletişime Geç", "Get in Touch"),

            ("home.featuresTitle", "Neden Biz?", "Why Us?"),
            ("home.feature1Title", "Çok Dilli", "Multilingual"),
            ("home.feature1Text", "Sınırsız dil, panelden yönetilir.", "Unlimited languages, managed from the panel."),
            ("home.feature2Title", "Yönetilebilir", "Manageable"),
            ("home.feature2Text", "Tüm içerik, logo ve renkler admin panelde.", "All content, logo and colors in the admin panel."),
            ("home.feature3Title", "Mail Hazır", "Mail Ready"),
            ("home.feature3Text", "SMTP ayarları ve iletişim formu kutudan çıkar.", "SMTP settings and contact form out of the box."),

            ("contactPage.formTitle", "Bize Yazın", "Write to Us"),
            ("contactPage.formName", "Ad Soyad", "Full Name"),
            ("contactPage.formEmail", "E-posta", "E-mail"),
            ("contactPage.formPhone", "Telefon", "Phone"),
            ("contactPage.formSubject", "Konu", "Subject"),
            ("contactPage.formMessage", "Mesajınız", "Your Message"),
            ("contactPage.formSubmit", "Mesajı Gönder", "Send Message"),
            ("contactPage.success", "Mesajınız alındı. En kısa sürede dönüş yapacağız.", "Your message has been received. We will get back to you shortly."),
            ("contactPage.infoTitle", "İletişim Bilgileri", "Contact Information"),
            ("contactPage.addressLabel", "Adres", "Address"),
            ("contactPage.phoneLabel", "Telefon", "Phone"),
            ("contactPage.emailLabel", "E-posta", "E-mail"),
            ("contactPage.hoursLabel", "Çalışma Saatleri", "Working Hours"),

            ("footer.about", "Modern, çok dilli, panelden yönetilen CMS altyapısı.", "A modern, multilingual, panel-managed CMS foundation."),
            ("footer.linksTitle", "Hızlı Menü", "Quick Links"),
            ("footer.contactTitle", "İletişim", "Contact"),
            ("footer.followTitle", "Bizi Takip Edin", "Follow Us"),
            ("footer.rights", "Tüm hakları saklıdır.", "All rights reserved."),

            ("whatsapp.tooltip", "Bize WhatsApp'tan yazın", "Message us on WhatsApp"),

            ("common.home", "Ana Sayfa", "Home"),
            ("common.readMore", "Devamı →", "Read more →"),
            ("common.previous", "Önceki", "Previous"),
            ("common.next", "Sonraki", "Next"),

            ("notFound.title", "Sayfa Bulunamadı", "Page Not Found"),
            ("notFound.text", "Aradığınız sayfa taşınmış veya hiç var olmamış olabilir.", "The page you are looking for may have moved or never existed."),
            ("notFound.home", "Ana Sayfaya Dön", "Back to Home"),

            ("seo.siteTitle", "Starter | Modern CMS", "Starter | Modern CMS"),
            ("seo.description", "Modern, çok dilli, panelden yönetilen CMS altyapısı.", "A modern, multilingual, panel-managed CMS foundation."),
            ("seo.keywords", "cms, çok dilli, asp.net core", "cms, multilingual, asp.net core"),
        };

        var existing = await db.LocalizationResources
            .Select(r => new { r.Key, r.LanguageCode })
            .ToListAsync();
        bool Has(string key, string lang) => existing.Any(e => e.Key == key && e.LanguageCode == lang);

        foreach (var (key, tr, en) in rows)
        {
            if (!Has(key, "tr")) db.LocalizationResources.Add(new LocalizationResource { Key = key, LanguageCode = "tr", Value = tr });
            if (!Has(key, "en")) db.LocalizationResources.Add(new LocalizationResource { Key = key, LanguageCode = "en", Value = en });
        }
        await db.SaveChangesAsync();
    }
}
