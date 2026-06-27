# Starter.Cms — Çok Dilli ASP.NET Core CMS Base

Yeni projelere **hızlı başlangıç** için hazır, %100 dinamik, çok dilli, admin panelli bir CMS
iskeleti. Mail servisi (SMTP), medya kütüphanesi, çok dillilik, marka/tema yönetimi, SEO ve hata
yönetimi **kutudan çıkar çıkmaz** hazır gelir. Yeni bir site yaparken bu base'i kopyalayıp içerik
modüllerini ekleyerek devam edersin — altyapıyı her seferinde yeniden yazmazsın.

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
cd Starter.Cms
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
| **İçerik** | `Page` (genel sayfa, çoklu layout), `Slide` (hero slider), `ContactMessage` (form mesajları) — dil sekmeli CRUD. |
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
Starter.Cms/
├── Domain/            BaseEntity + entity'ler + çeviri tabloları
├── Data/              DbContext · DbSeeder · DesignTime factory
├── Services/          Culture · Caching · FileStorage · Branding · Mail(SMTP) · ColorUtil ...
├── Localization/      DbStringLocalizer + LocalizationStore (DB destekli arayüz metinleri)
├── Controllers/       Home · Pages · Contact · Seo (sitemap/robots)
├── ViewComponents/    Header · Footer · Slider (DB beslemeli)
├── Views/             _Layout (SEO/marka/WhatsApp) · sayfa view'ları
├── Areas/Admin/       Panel: Dashboard · Pages · Slides · Branding · Mail · Media · Messages · Languages · Localization
├── Styles/app.css     Tailwind kaynağı  →  wwwroot/css/app.css (derlenmiş, depoda)
└── Program.cs         DI · route tabanlı kültür · routing
```

## Notlar

- **Tailwind çıktısı (`wwwroot/css/app.css`) depoya işlenir** → Node olmayan ortamda da site çalışır.
  `dotnet build`, `node_modules` varsa CSS'i best-effort tazeler.
- SQLite varsayılan; MSSQL'e geçiş `Program.cs`'te `UseSqlite` → `UseSqlServer` (tek satır) + connection string.
- Admin tek dilli (Türkçe); arayüz/içerik çok dilli.
