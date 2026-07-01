using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Domain;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Data;

/// <summary>
/// İlk açılışta AMT Vinç Platform örnek içeriğini (TR/EN) tohumlar. Her bölüm idempotenttir:
/// ilgili tablo/anahtar boşsa ekler, mevcut veriye dokunmaz. Tüm metin/görseller admin
/// panelden düzenlenebilir — bu yalnızca başlangıç içeriğidir.
/// </summary>
public static class DbSeeder
{
    // Başlangıç görselleri (Unsplash — vinç/şantiye). Admin panelden kendi görsellerinizle değiştirin.
    private const string Hero1 = "https://images.unsplash.com/photo-1503387762-592deb58ef4e?q=80&w=1600&auto=format&fit=crop";
    private const string Hero2 = "https://images.unsplash.com/photo-1541888946425-d81bb19240f5?q=80&w=1600&auto=format&fit=crop";
    private const string Mac1 = "https://images.unsplash.com/photo-1581094794329-c8112a89af12?q=80&w=1200&auto=format&fit=crop";
    private const string Mac2 = "https://images.unsplash.com/photo-1504328345606-18bbc8c9d7d1?q=80&w=1200&auto=format&fit=crop";

    public static async Task SeedAsync(ApplicationDbContext db, IConfiguration? config = null)
    {
        await SeedLanguagesAsync(db);
        await SeedLocalizationAsync(db);
        await SeedSettingsAsync(db);
        await SeedMenuAsync(db);
        await SeedSlidesAsync(db);
        await SeedServicesAsync(db);
        await SeedMachinesAsync(db);
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
        Add("/makineler", 2, "Makine Filosu", "Fleet");
        Add("/hizmetler", 3, "Hizmetler", "Services");
        Add("/about", 4, "Hakkımızda", "About");
        Add("/contact", 5, "İletişim", "Contact");
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
        Add("contact.email", "info@amtvinc.com", "contact");
        Add("whatsapp.number", "905000000000", "general");
        Add("social.facebook", "", "social");
        Add("social.instagram", "https://www.instagram.com/amtvinc.platform/", "social");
        Add("social.linkedin", "", "social");
        Add("social.youtube", "", "social");

        // Marka (logo boşsa firma adı metni gösterilir)
        Add("branding.companyName", "AMT Vinç Platform", "branding");
        Add("branding.logoUrl", "", "branding");
        Add("branding.logoLightUrl", "", "branding");
        Add("branding.faviconUrl", "/favicon.ico", "branding");
        Add("branding.address", "Örnek Mah. Sanayi Cad. No:1, İstanbul", "branding");
        Add("branding.workingHours", "7/24 Hizmet · Pzt - Cmt: 08:00 - 19:00", "branding");

        // Tema renkleri (güvenlik turuncusu + koyu lacivert — vinç/iş güvenliği sektörü)
        Add("theme.primary", "#ea580c", "theme");
        Add("theme.primaryDark", "#c2410c", "theme");
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
        Add("smtp.fromName", "AMT Vinç Platform", "smtp");
        Add("smtp.toEmail", "info@amtvinc.com", "smtp");

        // İletişim formu bildirim e-postası (panelden düzenlenebilir; {{...}} yer tutucuları)
        Add("mail.contactSubject", "[Teklif/İletişim] {{subject}}", "mail");
        Add("mail.contactTemplate", DefaultContactTemplate, "mail");

        await db.SaveChangesAsync();
    }

    private const string DefaultContactTemplate = """
        <div style="font-family:Arial,Helvetica,sans-serif;max-width:600px;margin:0 auto;border:1px solid #eee;border-radius:8px;overflow:hidden">
          <div style="background:#0f172a;color:#fff;padding:20px 24px">
            <h2 style="margin:0;font-size:18px">Yeni Teklif / İletişim Talebi</h2>
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
        var data = new (string Img, string KTr, string TTr, string STr, string CTr, string UrlTr,
                        string KEn, string TEn, string SEn, string CEn)[]
        {
            (Hero1, "YÜKSEKTE ÇALIŞMA ÇÖZÜMLERİ", "Vinç ve Platform Kiralamada Güvenilir Çözüm Ortağınız",
             "Manlift, makaslı ve eklemli platformlar ile sepetli vinçlerimizle her projeye uygun, güvenli yükseğe erişim.",
             "Hemen Teklif Al", "/contact",
             "WORKING AT HEIGHT SOLUTIONS", "Your Reliable Partner in Crane & Platform Rental",
             "Safe access to heights for every project with our manlifts, scissor & boom platforms and truck-mounted cranes.",
             "Get a Quote"),
            (Hero2, "GENİŞ MAKİNE FİLOSU", "12 m'den 45 m'ye Geniş Erişim Yelpazesi",
             "Bakımlı, sigortalı ve operatörlü/operatörsüz kiralanabilen güncel makine parkımızla yanınızdayız.",
             "Makine Filosu", "/makineler",
             "WIDE MACHINE FLEET", "A Broad Reach Range from 12 m to 45 m",
             "Well-maintained, insured machines available with or without an operator — our modern fleet at your service.",
             "View Fleet"),
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
                    new SlideTranslation { LanguageCode = "tr", Kicker = s.KTr, Title = s.TTr, Subtitle = s.STr, CtaText = s.CTr, CtaUrl = s.UrlTr },
                    new SlideTranslation { LanguageCode = "en", Kicker = s.KEn, Title = s.TEn, Subtitle = s.SEn, CtaText = s.CEn, CtaUrl = s.UrlTr }
                }
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedServicesAsync(ApplicationDbContext db)
    {
        if (await db.Services.AnyAsync()) return;

        var data = new (string Slug, string Icon, string TTr, string STr, string BTr,
                        string TEn, string SEn, string BEn)[]
        {
            ("manlift-kiralama", "move-vertical",
             "Manlift (Personel Yükseltici) Kiralama",
             "Dar alanlarda dahi yüksekte güvenli çalışma için akülü ve dizel manliftler.",
             "<p>Bakım, montaj, cephe ve tavan işlerinde personelinizi güvenle yükseğe taşıyan manlift çözümleri sunuyoruz. İç mekânlar için akülü (emisyonsuz), açık şantiyeler için dizel seçenekler mevcuttur.</p>",
             "Manlift (Personnel Lift) Rental",
             "Battery and diesel manlifts for safe work at height, even in tight spaces.",
             "<p>We provide manlift solutions that safely raise your personnel for maintenance, assembly, façade and ceiling work. Battery (zero-emission) options for indoors and diesel options for open sites are available.</p>"),

            ("makasli-platform-kiralama", "chevrons-up",
             "Makaslı Platform Kiralama",
             "Geniş çalışma alanı ve yüksek kapasite gerektiren işler için makaslı platformlar.",
             "<p>Düşey erişim gerektiren depo, AVM, fabrika ve inşaat işlerinde geniş sepet alanı ve yüksek taşıma kapasitesiyle verimli çalışma sağlar.</p>",
             "Scissor Lift Rental",
             "Scissor platforms for jobs needing a wide work area and high capacity.",
             "<p>Provides efficient work with a large platform area and high load capacity for warehouse, mall, factory and construction tasks requiring vertical access.</p>"),

            ("eklemli-platform-kiralama", "spline",
             "Eklemli Platform Kiralama",
             "Engellerin üzerinden ve yanından erişim sağlayan eklemli (örümcek) platformlar.",
             "<p>Eklemli bom yapısı sayesinde engellerin üzerinden uzanarak zor noktalara yandan erişim imkânı sunar. Cephe, ağaç bakımı ve endüstriyel bakım için idealdir.</p>",
             "Articulated Boom Rental",
             "Articulated (knuckle) platforms that reach over and around obstacles.",
             "<p>Thanks to the articulated boom, it reaches over obstacles to access hard-to-reach points from the side. Ideal for façades, tree care and industrial maintenance.</p>"),

            ("sepetli-vinc-kiralama", "truck",
             "Sepetli Vinç Kiralama",
             "Yüksek erişim ve hareket kabiliyeti için araç üstü sepetli vinçler.",
             "<p>Şehir içi ve şantiye işlerinde hızlı konumlanan, yüksek erişimli araç üstü sepetli vinçlerle aydınlatma, tabela, budama ve bakım işlerinizi güvenle tamamlayın.</p>",
             "Truck-Mounted Platform Rental",
             "Truck-mounted aerial platforms for high reach and mobility.",
             "<p>With high-reach, quickly positioned truck-mounted platforms, complete your lighting, signage, pruning and maintenance work safely in the city and on site.</p>"),

            ("operatorlu-kiralama", "hard-hat",
             "Operatörlü Kiralama",
             "Deneyimli ve sertifikalı operatörlerle anahtar teslim yüksekte çalışma.",
             "<p>İş güvenliği eğitimli, sertifikalı operatörlerimizle makineyi siz düşünmeyin. Projenize özel operatörlü kiralama ile zaman ve güvenlik kazanın.</p>",
             "Rental with Operator",
             "Turnkey work at height with experienced, certified operators.",
             "<p>With our safety-trained, certified operators, leave the machine to us. Save time and ensure safety with operator-included rental tailored to your project.</p>"),

            ("uzun-donem-kiralama", "calendar-clock",
             "Uzun Dönem Kiralama",
             "Aylık ve yıllık projeleriniz için avantajlı uzun dönem kiralama.",
             "<p>Uzun süreli şantiye ve bakım projeleriniz için bütçe dostu aylık/yıllık kiralama anlaşmaları sunuyoruz. Bakım ve yedek makine desteği dahildir.</p>",
             "Long-Term Rental",
             "Advantageous long-term rental for your monthly and yearly projects.",
             "<p>We offer budget-friendly monthly/yearly rental agreements for your long-term site and maintenance projects. Maintenance and backup machine support included.</p>"),
        };

        int order = 1;
        foreach (var s in data)
        {
            db.Services.Add(new Service
            {
                Slug = s.Slug,
                Icon = s.Icon,
                SortOrder = order++,
                Translations =
                {
                    new ServiceTranslation { LanguageCode = "tr", Title = s.TTr, Summary = s.STr, Body = s.BTr, MetaTitle = s.TTr, MetaDescription = s.STr },
                    new ServiceTranslation { LanguageCode = "en", Title = s.TEn, Summary = s.SEn, Body = s.BEn, MetaTitle = s.TEn, MetaDescription = s.SEn }
                }
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedMachinesAsync(ApplicationDbContext db)
    {
        if (await db.MachineCategories.AnyAsync()) return;

        MachineCategory Cat(string slug, int order, string tr, string en) => new()
        {
            Slug = slug,
            SortOrder = order,
            Translations =
            {
                new MachineCategoryTranslation { LanguageCode = "tr", Name = tr },
                new MachineCategoryTranslation { LanguageCode = "en", Name = en }
            }
        };

        var makasli = Cat("makasli-platform", 1, "Makaslı Platformlar", "Scissor Lifts");
        var eklemli = Cat("eklemli-platform", 2, "Eklemli Platformlar", "Articulated Booms");
        var teleskopik = Cat("teleskopik-platform", 3, "Teleskopik Platformlar", "Telescopic Booms");
        var sepetli = Cat("sepetli-vinc", 4, "Sepetli Vinçler", "Truck-Mounted Platforms");
        db.MachineCategories.AddRange(makasli, eklemli, teleskopik, sepetli);

        void Add(MachineCategory cat, int order, bool featured, string img, string slug,
                 string wh, string cap, string reach, string weight,
                 string nameTr, string sdTr, string nameEn, string sdEn) =>
            cat.Machines.Add(new Machine
            {
                Slug = slug,
                ImageUrl = img,
                SortOrder = order,
                IsFeatured = featured,
                WorkingHeight = wh,
                Capacity = cap,
                Reach = reach,
                Weight = weight,
                Translations =
                {
                    new MachineTranslation { LanguageCode = "tr", Name = nameTr, ShortDescription = sdTr,
                        Description = $"<p>{sdTr} Tüm makinelerimiz periyodik bakımlı, sigortalı ve iş güvenliği standartlarına uygundur. Operatörlü veya operatörsüz, kısa ve uzun dönem kiralanabilir.</p>" },
                    new MachineTranslation { LanguageCode = "en", Name = nameEn, ShortDescription = sdEn,
                        Description = $"<p>{sdEn} All our machines are regularly maintained, insured and compliant with occupational safety standards. Available with or without an operator, for short and long term.</p>" }
                }
            });

        Add(makasli, 1, true, Mac1, "12m-makasli-platform", "12 m", "320 kg", "", "2.900 kg",
            "12 m Makaslı Platform", "Depo ve iç mekân işleri için kompakt makaslı platform.",
            "12 m Scissor Lift", "Compact scissor lift for warehouse and indoor work.");
        Add(makasli, 2, false, Mac2, "16m-makasli-platform", "16 m", "500 kg", "", "4.200 kg",
            "16 m Makaslı Platform", "Yüksek kapasiteli, geniş sepetli makaslı platform.",
            "16 m Scissor Lift", "High-capacity scissor lift with a wide platform.");

        Add(eklemli, 1, true, Mac1, "16m-eklemli-platform", "16 m", "230 kg", "8 m", "6.900 kg",
            "16 m Eklemli Platform", "Engel üzeri yan erişim sağlayan eklemli platform.",
            "16 m Articulated Boom", "Articulated boom for side reach over obstacles.");
        Add(eklemli, 2, false, Mac2, "20m-eklemli-platform", "20 m", "230 kg", "9 m", "8.400 kg",
            "20 m Eklemli Platform", "Orta yükseklikte geniş çalışma yarıçapı.",
            "20 m Articulated Boom", "Wide working radius at medium height.");

        Add(teleskopik, 1, true, Mac1, "22m-teleskopik-platform", "22 m", "230 kg", "18 m", "10.900 kg",
            "22 m Teleskopik Platform", "Uzun yatay erişimli teleskopik bom platformu.",
            "22 m Telescopic Boom", "Telescopic boom with long horizontal reach.");
        Add(teleskopik, 2, false, Mac2, "28m-teleskopik-platform", "28 m", "230 kg", "23 m", "14.500 kg",
            "28 m Teleskopik Platform", "Yüksek ve uzak noktalar için teleskopik platform.",
            "28 m Telescopic Boom", "Telescopic boom for high and distant points.");

        Add(sepetli, 1, true, Mac2, "30m-sepetli-vinc", "30 m", "200 kg", "20 m", "",
            "30 m Sepetli Vinç", "Araç üstü, hızlı konumlanan yüksek erişimli sepetli vinç.",
            "30 m Truck Platform", "Truck-mounted, quickly positioned high-reach platform.");
        Add(sepetli, 2, false, Mac1, "45m-sepetli-vinc", "45 m", "300 kg", "30 m", "",
            "45 m Sepetli Vinç", "Çok katlı cephe ve endüstriyel işler için yüksek sepetli vinç.",
            "45 m Truck Platform", "High truck platform for multi-storey façades and industry.");

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
                    Lead = "Yüksekte çalışma ve kaldırma çözümlerinde güvenilir iş ortağınız.",
                    Body = "<p><strong>AMT Vinç Platform</strong>, manlift, makaslı ve eklemli platformlar ile sepetli vinç kiralama alanında hizmet veren bir firmadır. Bakımlı ve sigortalı makine parkımız, sertifikalı operatörlerimiz ve iş güvenliği önceliğimizle inşaat, sanayi, lojistik ve bakım projelerinde yanınızdayız.</p><p>Amacımız; doğru makineyi, doğru zamanda ve en güvenli şekilde projenize ulaştırmaktır. Kısa ve uzun dönem, operatörlü veya operatörsüz kiralama seçeneklerimizle ihtiyacınıza uygun çözümü sunuyoruz.</p>",
                    MetaTitle = "Hakkımızda | AMT Vinç Platform",
                    MetaDescription = "AMT Vinç Platform — manlift, makaslı/eklemli platform ve sepetli vinç kiralama hizmetleri."
                },
                new PageTranslation
                {
                    LanguageCode = "en",
                    Title = "About Us",
                    Lead = "Your reliable partner in work-at-height and lifting solutions.",
                    Body = "<p><strong>AMT Vinç Platform</strong> provides manlift, scissor & articulated platform and truck-mounted crane rental services. With our maintained and insured fleet, certified operators and safety-first approach, we support construction, industry, logistics and maintenance projects.</p><p>Our goal is to deliver the right machine, at the right time and in the safest way for your project. We offer short and long-term, with or without operator rental options tailored to your needs.</p>",
                    MetaTitle = "About Us | AMT Vinç Platform",
                    MetaDescription = "AMT Vinç Platform — manlift, scissor/articulated platform and truck-mounted crane rental services."
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
                new PageTranslation { LanguageCode = "tr", Title = "İletişim", Lead = "Teklif ve bilgi için bize ulaşın.", Body = "" },
                new PageTranslation { LanguageCode = "en", Title = "Contact", Lead = "Get in touch for a quote or info.", Body = "" }
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
            ("nav.cta", "Teklif Al", "Get a Quote"),

            // Ana sayfa — hizmetler bölümü
            ("home.servicesKicker", "Hizmetlerimiz", "Our Services"),
            ("home.servicesTitle", "Yüksekte Çalışmanın Her Türü İçin Çözüm", "A Solution for Every Type of Work at Height"),
            ("home.servicesSubtitle", "Manliftten sepetli vince kadar geniş hizmet yelpazemizle projenize en uygun erişim çözümünü sunuyoruz.", "From manlifts to truck platforms, we offer the access solution that best fits your project."),

            // Ana sayfa — makineler bölümü
            ("home.machinesKicker", "Makine Filosu", "Our Fleet"),
            ("home.machinesTitle", "Öne Çıkan Makinelerimiz", "Featured Machines"),
            ("home.machinesSubtitle", "12 m'den 45 m'ye kadar bakımlı ve sigortalı geniş makine parkımızdan bazıları.", "A selection from our maintained, insured fleet ranging from 12 m to 45 m."),
            ("home.allMachines", "Tüm Filoyu İncele", "Browse Full Fleet"),

            // Ana sayfa — neden biz
            ("home.featuresTitle", "Neden AMT Vinç Platform?", "Why AMT Vinç Platform?"),
            ("home.feature1Title", "İş Güvenliği Önceliği", "Safety First"),
            ("home.feature1Text", "Sertifikalı operatörler ve düzenli bakımlı makinelerle güvenli çalışma.", "Safe operation with certified operators and regularly maintained machines."),
            ("home.feature2Title", "Hızlı Teslimat", "Fast Delivery"),
            ("home.feature2Text", "Şantiyenize zamanında ulaşan makine ve esnek kiralama süreleri.", "Machines that arrive on time, with flexible rental periods."),
            ("home.feature3Title", "Geniş Filo", "Wide Fleet"),
            ("home.feature3Text", "Her projeye uygun yükseklik ve kapasitede makine seçeneği.", "Machine options at the right height and capacity for every project."),
            ("home.feature4Title", "7/24 Destek", "24/7 Support"),
            ("home.feature4Text", "Kiralama süresince teknik destek ve yedek makine güvencesi.", "Technical support and backup-machine assurance throughout the rental."),

            // Hizmetler sayfası
            ("services.title", "Hizmetlerimiz", "Our Services"),
            ("services.subtitle", "Yüksekte çalışma ve kaldırma ihtiyaçlarınız için kapsamlı kiralama hizmetleri.", "Comprehensive rental services for your work-at-height and lifting needs."),

            // Makineler sayfası
            ("machines.title", "Makine Filosu", "Machine Fleet"),
            ("machines.subtitle", "Kategorilere göre bakımlı ve sigortalı makine parkımız. Detay ve teklif için makineye tıklayın.", "Our maintained, insured fleet by category. Click a machine for details and a quote."),
            ("machines.specHeight", "Çalışma Yük.", "Work Height"),
            ("machines.specCapacity", "Kapasite", "Capacity"),
            ("machines.specReach", "Yatay Erişim", "Reach"),
            ("machines.specWeight", "Ağırlık", "Weight"),
            ("machines.backToList", "Filoya Dön", "Back to Fleet"),

            // CTA
            ("cta.quoteTitle", "Bu Hizmet İçin Teklif Alın", "Get a Quote for This Service"),
            ("cta.quoteText", "İhtiyacınıza en uygun makineyi birlikte belirleyelim. Hızlı ve ücretsiz teklif için bize ulaşın.", "Let's find the most suitable machine together. Contact us for a fast, free quote."),
            ("cta.quoteButton", "Teklif Al", "Get a Quote"),
            ("cta.homeTitle", "Projeniz İçin Doğru Makineyi Bulalım", "Let's Find the Right Machine for Your Project"),
            ("cta.homeText", "Yüksekte çalışma ihtiyacınızı bize iletin; en uygun makineyi ve fiyatı birlikte belirleyelim.", "Tell us your work-at-height needs and we'll determine the best machine and price together."),

            // İletişim
            ("contactPage.formTitle", "Teklif / İletişim Formu", "Quote / Contact Form"),
            ("contactPage.formName", "Ad Soyad", "Full Name"),
            ("contactPage.formEmail", "E-posta", "E-mail"),
            ("contactPage.formPhone", "Telefon", "Phone"),
            ("contactPage.formSubject", "Konu", "Subject"),
            ("contactPage.formMessage", "Mesajınız", "Your Message"),
            ("contactPage.formSubmit", "Gönder", "Send"),
            ("contactPage.success", "Talebiniz alındı. En kısa sürede dönüş yapacağız.", "Your request has been received. We will get back to you shortly."),
            ("contactPage.infoTitle", "İletişim Bilgileri", "Contact Information"),
            ("contactPage.addressLabel", "Adres", "Address"),
            ("contactPage.phoneLabel", "Telefon", "Phone"),
            ("contactPage.emailLabel", "E-posta", "E-mail"),
            ("contactPage.hoursLabel", "Çalışma Saatleri", "Working Hours"),

            // Footer
            ("footer.about", "Manlift, makaslı/eklemli platform ve sepetli vinç kiralamada güvenilir çözüm ortağınız.", "Your reliable partner in manlift, scissor/articulated platform and truck-mounted crane rental."),
            ("footer.linksTitle", "Hızlı Menü", "Quick Links"),
            ("footer.contactTitle", "İletişim", "Contact"),
            ("footer.followTitle", "Bizi Takip Edin", "Follow Us"),
            ("footer.rights", "Tüm hakları saklıdır.", "All rights reserved."),

            ("whatsapp.tooltip", "WhatsApp'tan teklif alın", "Get a quote on WhatsApp"),

            ("common.home", "Ana Sayfa", "Home"),
            ("common.readMore", "Detaylı Bilgi →", "Learn more →"),
            ("common.viewAll", "Tüm Hizmetler", "All Services"),
            ("common.emptyContent", "İçerik yakında eklenecektir.", "Content will be added soon."),

            ("notFound.title", "Sayfa Bulunamadı", "Page Not Found"),
            ("notFound.text", "Aradığınız sayfa taşınmış veya hiç var olmamış olabilir.", "The page you are looking for may have moved or never existed."),
            ("notFound.home", "Ana Sayfaya Dön", "Back to Home"),

            ("seo.siteTitle", "AMT Vinç Platform | Vinç ve Platform Kiralama", "AMT Vinç Platform | Crane & Platform Rental"),
            ("seo.description", "Manlift, makaslı/eklemli platform ve sepetli vinç kiralama. Operatörlü/operatörsüz, kısa ve uzun dönem.", "Manlift, scissor/articulated platform and truck-mounted crane rental. With/without operator, short and long term."),
            ("seo.keywords", "vinç kiralama, platform kiralama, manlift, makaslı platform, sepetli vinç, yüksekte çalışma", "crane rental, platform rental, manlift, scissor lift, boom lift, work at height"),
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
