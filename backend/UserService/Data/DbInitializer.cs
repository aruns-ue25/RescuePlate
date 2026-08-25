using Npgsql;
using UserService.Models;

namespace UserService.Data;

public static class DbInitializer
{
    public static void Initialize(string connectionString, IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
            var targetDatabase = builder.Database;

            // 1. Connect to default 'postgres' database to check if target database exists
            builder.Database = "postgres";
            using (var masterConn = new NpgsqlConnection(builder.ConnectionString))
            {
                masterConn.Open();
                using var cmd = masterConn.CreateCommand();
                cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{targetDatabase}'";
                var exists = cmd.ExecuteScalar() != null;

                if (!exists)
                {
                    logger.LogInformation("Database '{TargetDatabase}' does not exist. Creating automatically...", targetDatabase);
                    using var createCmd = masterConn.CreateCommand();
                    createCmd.CommandText = $"CREATE DATABASE \"{targetDatabase}\"";
                    createCmd.ExecuteNonQuery();
                    logger.LogInformation("Database '{TargetDatabase}' created successfully.", targetDatabase);
                }
            }

            // 2. Run EF Core EnsureCreated & Seed Admin
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
            db.Database.EnsureCreated();

            if (!db.Users.Any(u => u.Email == "admin@rescueplate.org"))
            {
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@rescueplate.org",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 11),
                    Role = UserRole.ADMIN,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Users.Add(adminUser);
                db.SaveChanges();
                logger.LogInformation("Admin account seeded (admin@rescueplate.org / Admin@123).");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize PostgreSQL database.");
            throw;
        }
    }
}
