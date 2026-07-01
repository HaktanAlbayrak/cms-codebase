using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using AmtVinc.Cms.Data;
using AmtVinc.Cms.Domain;

namespace AmtVinc.Cms.Services;

/// <summary>
/// Admin oturum yönetimi. Controller'lar HttpContext'e doğrudan değil bu servise
/// bağımlıdır (aracı oturum servisi). Kullanıcılar veritabanından doğrulanır; şifreler
/// PBKDF2 ile hash'lenir. Pasif (IsActive=false) kullanıcılar giriş yapamaz.
/// </summary>
public interface IAdminAuthService
{
    public const string Scheme = "AdminAuth";
    Task<bool> SignInAsync(HttpContext http, string username, string password);
    Task SignOutAsync(HttpContext http);
}

public class AdminAuthService : IAdminAuthService
{
    private readonly ApplicationDbContext _db;

    public AdminAuthService(ApplicationDbContext db) => _db = db;

    public async Task<bool> SignInAsync(HttpContext http, string username, string password)
    {
        username = (username ?? "").Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null || !user.IsActive || !PasswordHasher.Verify(password, user.PasswordHash))
            return false;

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, IAdminAuthService.Scheme);
        await http.SignInAsync(IAdminAuthService.Scheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
        return true;
    }

    public Task SignOutAsync(HttpContext http) => http.SignOutAsync(IAdminAuthService.Scheme);
}
