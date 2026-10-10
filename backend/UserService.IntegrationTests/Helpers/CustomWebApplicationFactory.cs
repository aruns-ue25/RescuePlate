using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UserService.Data;
using UserService.Models;

namespace UserService.IntegrationTests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _sqliteConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminSettings:Email"] = "admin@rescueplate.org",
                ["AdminSettings:AccessKey"] = "ADMIN-SECURE-KEY-2026",
                ["AdminSettings:ChallengeTokenExpiryMinutes"] = "5",
                ["AdminSettings:MaxFailedAttempts"] = "5",
                ["AdminSettings:LockoutMinutes"] = "15",
                ["Jwt:SecretKey"] = "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood",
                ["Jwt:Issuer"] = "RescuePlate.UserService",
                ["Jwt:Audience"] = "RescuePlate.Client"
            });
        });

        builder.ConfigureServices(services =>
        {
            var efDescriptors = services.Where(d =>
                (d.ServiceType.Namespace != null && d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore")) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DbContextOptions<RescuePlateDbContext>) ||
                d.ServiceType == typeof(RescuePlateDbContext)).ToList();

            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RescuePlateDbContext>(options =>
            {
                options.UseSqlite(_sqliteConnection);
            });

            // Ensure schema and admin user are initialized
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
            db.Database.EnsureCreated();

            if (!db.Users.Any(u => u.Email == "admin@rescueplate.org"))
            {
                db.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@rescueplate.org",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 11),
                    Role = UserRole.ADMIN,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
        
        // Ensure Database created
        db.Database.EnsureCreated();

        // Remove all extra users except the default admin account
        var extraUsers = await db.Users.Where(u => u.Email != "admin@rescueplate.org").ToListAsync();
        if (extraUsers.Count > 0)
        {
            db.Users.RemoveRange(extraUsers);
        }

        // Ensure default admin user exists
        if (!await db.Users.AnyAsync(u => u.Email == "admin@rescueplate.org"))
        {
            db.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@rescueplate.org",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 11),
                Role = UserRole.ADMIN,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _sqliteConnection?.Close();
            _sqliteConnection?.Dispose();
        }
    }
}
