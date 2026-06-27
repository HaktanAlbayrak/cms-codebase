using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Starter.Cms.Services;

namespace Starter.Cms.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = IAdminAuthService.Scheme)]
[TypeFilter(typeof(AdminExceptionFilter))]
public abstract class AdminControllerBase : Controller
{
}
