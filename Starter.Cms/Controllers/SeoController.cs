using System.Text;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Starter.Cms.Data;
using Starter.Cms.Services;

namespace Starter.Cms.Controllers;

/// <summary>
/// Arama motorları için <c>robots.txt</c> ve çok dilli <c>sitemap.xml</c> üretir
/// (kültür öneksiz kök endpoint'ler). Sitemap her dil için tüm aktif sayfaları ve
/// her URL için <c>hreflang</c> alternate'lerini içerir.
/// </summary>
public class SeoController : Controller
{
    private readonly ApplicationDbContext _db;

    public SeoController(ApplicationDbContext db) => _db = db;

    [Route("robots.txt")]
    public IActionResult Robots()
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Allow: /");
        sb.AppendLine("Disallow: /admin");
        sb.AppendLine($"Sitemap: {host}/sitemap.xml");
        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }

    [Route("sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var host = $"{Request.Scheme}://{Request.Host}";
        var cultures = CultureContext.Supported;

        // Statik üst sayfalar + DB'deki aktif içerik sayfaları (slug bazlı).
        var pageSlugs = await _db.Pages.Where(p => p.IsActive).Select(p => p.Slug).ToListAsync();
        var paths = new List<string> { "" };               // ana sayfa
        paths.AddRange(pageSlugs.Select(s => "/" + s));     // /about, /contact ...

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };
        await using var sw = new StringWriter(sb);
        using (var w = XmlWriter.Create(sw, settings))
        {
            w.WriteStartDocument();
            w.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
            w.WriteAttributeString("xmlns", "xhtml", null, "http://www.w3.org/1999/xhtml");

            foreach (var culture in cultures)
            {
                foreach (var path in paths)
                {
                    w.WriteStartElement("url");
                    w.WriteElementString("loc", $"{host}/{culture}{path}");
                    w.WriteElementString("changefreq", "weekly");

                    // Aynı sayfanın tüm dil alternate'leri (hreflang).
                    foreach (var alt in cultures)
                    {
                        w.WriteStartElement("xhtml", "link", "http://www.w3.org/1999/xhtml");
                        w.WriteAttributeString("rel", "alternate");
                        w.WriteAttributeString("hreflang", alt);
                        w.WriteAttributeString("href", $"{host}/{alt}{path}");
                        w.WriteEndElement();
                    }
                    // x-default → varsayılan dil.
                    w.WriteStartElement("xhtml", "link", "http://www.w3.org/1999/xhtml");
                    w.WriteAttributeString("rel", "alternate");
                    w.WriteAttributeString("hreflang", "x-default");
                    w.WriteAttributeString("href", $"{host}/{CultureContext.Default}{path}");
                    w.WriteEndElement();

                    w.WriteEndElement(); // url
                }
            }

            w.WriteEndElement();
            w.WriteEndDocument();
        }

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}
