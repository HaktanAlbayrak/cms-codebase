# AMT Vinç Platform — Çok Dilli ASP.NET Core CMS

**AMT Vinç Platform** (vinç & yüksekte çalışma platformu kiralama) için %100 dinamik, çok dilli
(TR/EN), admin panelli kurumsal web sitesi. `Starter.Cms` base'i üzerine kurulmuştur; sektöre özel
**Makine Filosu** (kategori + makine, teknik özellikler) ve **Hizmetler** modülleri eklenmiştir.
Mail servisi (SMTP), medya kütüphanesi, marka/tema yönetimi, SEO ve hata yönetimi kutudan çıkar
çıkmaz hazır gelir. **Tüm içerik/görsel/metin admin panelden yönetilir** — sıfır statik içerik.

> İçerik başlangıç olarak vinç/platform sektörüne uygun örnek verilerle tohumlanır; gerçek
> metin ve görseller admin panelden (`/admin`) girilir. İçerik kaynağı:
> [instagram.com/amtvinc.platform](https://www.instagram.com/amtvinc.platform/).

> Mimari: `html-to-cms` skill'inin kanıtlanmış desenleri (Alupunch.CmsV2 + quanqi + birleşik-makine
> projelerinden damıtıldı). **Çeviri-tablosu** deseni (EAV değil): her entity için ayrı
> `XTranslation` tablosu.

---

## Teknoloji

- **.NET 10** · ASP.NET Core MVC · EF Core · **SQLite** (sıfır kurulum)
- **Tailwind CSS** (CLI ile derlenir, Play CDN değil) · `lucide` ikonlar · `lightgallery`
- Cookie tabanlı admin auth · DB destekli `IStringLocalizer` · route tabanlı kültür (`/tr`, `/en`)

## Hızlı Başlangıç

```bash
cd AmtVinc.Cms
npm install          # Tailwind (bir kez)
npm run build:css    # wwwroot/css/app.css üret (CSS değişince tekrar; veya: npm run watch:css)
dotnet run           # DB oluşur + tohumlanır, site açılır
```

- Site: `http://localhost:5080/tr`
- Admin: `http://localhost:5080/admin` — varsayılan **admin / admin123**
  (`appsettings.json` → `Admin:Username/Password`'tan değiştir).

İlk çalıştırmada SQLite veritabanı migration'la oluşturulur ve örnek içerikle (TR/EN) tohumlanır.

## Hazır Gelen Özellikler

| Alan | Açıklama |
|---|---|
| **Çok dillilik** | Diller DB'de; yeni dil panelden eklenir (kod değişmeden), metinler varsayılandan tohumlanır. RTL hazır. |
| **Mail / SMTP** | Ayarlar DB'de (admin panel), mail şablonu + **test e-postası**. İletişim formu best-effort gerçek mail atar. |
| **Medya kütüphanesi** | Tek yükleme deposu; her görsel/PDF alanında "Medyadan Seç". CKEditor yüklemeleri de buraya düşer. |
| **Marka & Tema** | Logo, favicon, renk, iletişim, sosyal, SEO — hepsi panelden (sıfır statik). Marka rengi siteye anında yansır. |
| **Makine Filosu** | `MachineCategory` + `Machine` — kategori bazlı filo; teknik özellikler (çalışma yük., kapasite, erişim, ağırlık), öne çıkarma, dil sekmeli CRUD. `/tr/makineler` + `/tr/makine/{slug}`. |
| **Hizmetler** | `Service` — ikon + görsel + dil sekmeli içerik. Ana sayfa grid'i + `/tr/hizmetler` + `/tr/hizmet/{slug}`. |
| **Genel İçerik** | `Page` (çoklu layout), `Slide` (hero slider), `ContactMessage` (teklif/iletişim mesajları) — dil sekmeli CRUD. |
| **SEO** | Canonical, `hreflang` alternate + `x-default`, Open Graph, Twitter Card, JSON-LD (Organization), çok dilli `sitemap.xml`, `robots.txt`. |
| **Hata yönetimi** | Lokalize, DB'siz 404/403/500 sayfaları; admin hataları in-page banner. |

## Yeni Projeye Başlarken (yeniden adlandırma)

`Starter` / `Starter.Cms` adını kendi projene çevir:

1. Klasör `Starter.Cms` → `SeninProje.Cms`, dosya `Starter.Cms.csproj`, `Starter.slnx`.
2. Bul-değiştir: `Starter.Cms` → `SeninProje.Cms` (namespace), `namespace Starter.Cms` kökü.
3. `appsettings.json` connection string + admin şifresi.
4. Tohum (`Data/DbSeeder.cs`): firma adı, diller, örnek içerik.
5. `dotnet ef migrations add Initial` (mevcut `Migrations/` klasörünü silersen) veya olduğu gibi kullan.

## Yeni İçerik Tipi Ekleme (ör. Ürün)

1. `Domain/Urun.cs` + `UrunTranslation` (çeviri-tablosu deseni — `Page.cs`'i örnek al).
2. `ApplicationDbContext`'e `DbSet` + index + cascade ilişki.
3. `dotnet ef migrations add AddUrun` → migration.
4. `Services/UrunService.cs` (interface + cache'li) + `Program.cs`'te DI kaydı.
5. Frontend controller/view + admin controller/view (dil sekmeli; `Pages`'i kopyala).

## Proje Yapısı

```
AmtVinc.Cms/
├── Domain/            BaseEntity + entity'ler + çeviri tabloları (Machine · Service · Page · Slide ...)
├── Data/              DbContext · DbSeeder · DesignTime factory
├── Services/          Culture · Caching · FileStorage · Branding · Mail(SMTP) · MachineService · ServiceCatalogService ...
├── Localization/      DbStringLocalizer + LocalizationStore (DB destekli arayüz metinleri)
├── Controllers/       Home · Machines · Services · Pages · Contact · Seo (sitemap/robots)
├── ViewComponents/    Header · Footer · Slider (DB beslemeli)
├── Views/             _Layout (SEO/marka/WhatsApp) · Machines · Services · sayfa view'ları
├── Areas/Admin/       Panel: Dashboard · Machines · MachineCategories · Services · Pages · Slides · Branding · Mail · Media · Messages · Languages · Localization
├── Styles/app.css     Tailwind kaynağı  →  wwwroot/css/app.css (derlenmiş, depoda)
└── Program.cs         DI · route tabanlı kültür · routing
```

## Notlar

- **Tailwind çıktısı (`wwwroot/css/app.css`) depoya işlenir** → Node olmayan ortamda da site çalışır.
  `dotnet build`, `node_modules` varsa CSS'i best-effort tazeler.
- SQLite varsayılan; MSSQL'e geçiş `Program.cs`'te `UseSqlite` → `UseSqlServer` (tek satır) + connection string.
- Admin tek dilli (Türkçe); arayüz/içerik çok dilli.
