using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using UserService.Data;
using UserService.Models;

namespace UserService.IntegrationTests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Dedicated isolated test database name - NEVER touches RescuePlateDB
    public const string TestDatabaseName = "RescuePlate_IntegrationTestDB";
    public const string TestConnectionString = "Host=localhost;Port=5432;Database=RescuePlate_IntegrationTestDB;Username=postgres;Password=postgres";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        TryEnsurePostgresTestDb();

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQLConnection"] = TestConnectionString,
                ["Jwt:SecretKey"] = "RescuePlate_Super_Secret_Key_For_Jwt_Authentication_2026_Sprint1_RescueFood",
                ["Jwt:Issuer"] = "RescuePlate.UserService",
                ["Jwt:Audience"] = "RescuePlate.Client"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RescuePlateDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RescuePlateDbContext>(options =>
            {
                options.UseNpgsql(TestConnectionString);
            });
        });
    }

    private static bool TryEnsurePostgresTestDb()
    {
        try
        {
            var masterConnStr = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";
            using var conn = new NpgsqlConnection(masterConnStr);
            conn.Open();

            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{TestDatabaseName}'";
            var exists = checkCmd.ExecuteScalar() != null;

            if (!exists)
            {
                using var createCmd = conn.CreateCommand();
                createCmd.CommandText = $"CREATE DATABASE \"{TestDatabaseName}\"";
                createCmd.ExecuteNonQuery();
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
        
        // Remove non-admin users and associated profiles
        var nonAdminUsers = await db.Users.Where(u => u.Role != UserRole.ADMIN).ToListAsync();
        if (nonAdminUsers.Count > 0)
        {
            db.Users.RemoveRange(nonAdminUsers);
            await db.SaveChangesAsync();
        }
    }
}
