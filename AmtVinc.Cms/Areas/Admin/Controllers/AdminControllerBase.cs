using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmtVinc.Cms.Services;

namespace AmtVinc.Cms.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = IAdminAuthService.Scheme)]
[TypeFilter(typeof(AdminExceptionFilter))]
public abstract class AdminControllerBase : Controller
{
}
