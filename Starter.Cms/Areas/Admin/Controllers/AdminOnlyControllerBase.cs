using Microsoft.AspNetCore.Authorization;
using Starter.Cms.Domain;

namespace Starter.Cms.Areas.Admin.Controllers;

/// <summary>
/// Yalnızca <c>Admin</c> rolüne açık controller'lar için temel sınıf. Sistem ayarları
/// (marka, mail, dil, arayüz metinleri) ve kullanıcı yönetimi bundan türer. Content Manager
/// bu sayfalara eriştiğinde 403 → "Yetkisiz Erişim" sayfasına yönlendirilir.
/// </summary>
[Authorize(AuthenticationSchemes = Services.IAdminAuthService.Scheme, Roles = Roles.Admin)]
public abstract class AdminOnlyControllerBase : AdminControllerBase
{
}
