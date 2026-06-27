using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Starter.Cms.Data;

/// <summary>
/// EF Core araçlarının (dotnet ef migrations add ...) tasarım zamanında bir
/// DbContext üretebilmesi için fabrika. Uygulamayı çalıştırmaya gerek kalmaz.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=starter-cms.db")
            .Options;
        return new ApplicationDbContext(options);
    }
}
