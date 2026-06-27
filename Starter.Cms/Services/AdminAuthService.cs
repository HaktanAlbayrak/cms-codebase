using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Starter.Cms.Services;

/// <summary>
/// Admin oturum yönetimi. Controller'lar HttpContext'e doğrudan değil bu servise
/// bağımlıdır (aracı oturum servisi). Kullanıcı adı/şifre appsettings.json'dan okunur.
/// </summary>
public interface IAdminAuthService
{
    public const string Scheme = "AdminAuth";
    Task<bool> SignInAsync(HttpContext http, string username, string password);
    Task SignOutAsync(HttpContext http);
}

public class AdminAuthService : IAdminAuthService
{
    private readonly string _username;
    private readonly string _password;

    public AdminAuthService(IConfiguration config)
    {
        _username = config["Admin:Username"] ?? "admin";
        _password = config["Admin:Password"] ?? "admin123";
    }

    public async Task<bool> SignInAsync(HttpContext http, string username, string password)
    {
        if (!string.Equals(username, _username, StringComparison.OrdinalIgnoreCase) || password != _password)
            return false;

        var claims = new List<Claim> { new(ClaimTypes.Name, username), new(ClaimTypes.Role, "Admin") };
        var identity = new ClaimsIdentity(claims, IAdminAuthService.Scheme);
        await http.SignInAsync(IAdminAuthService.Scheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
        return true;
    }

    public Task SignOutAsync(HttpContext http) => http.SignOutAsync(IAdminAuthService.Scheme);
}
