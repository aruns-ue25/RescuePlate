using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using DonationService.Data;
using DonationService.Kafka;

namespace DonationService.IntegrationTests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public string ConnectionString { get; }
    public TestDonationEventProducer TestEventProducer { get; } = new();

    public CustomWebApplicationFactory()
    {
        ConnectionString = IntegrationTestConfig.GetConnectionString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        EnsureDatabaseCreated();

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQLConnection"] = ConnectionString,
                ["Jwt:SecretKey"] = AuthHelper.DefaultSecretKey,
                ["Jwt:Issuer"] = AuthHelper.DefaultIssuer,
                ["Jwt:Audience"] = AuthHelper.DefaultAudience,
                ["Kafka:EnableConsumer"] = "false",
                ["Kafka:BootstrapServers"] = "localhost:9092",
                ["Kafka:Topic"] = "donation-events-integrationtest"
            });
        });

        builder.ConfigureServices(services =>
        {
            // 1. Swap DbContext to use the test PostgreSQL database connection string
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DonationDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            services.AddDbContext<DonationDbContext>(options =>
            {
                options.UseNpgsql(ConnectionString);
            });

            // 2. Replace IDonationEventProducer with TestDonationEventProducer (Kafka Isolation)
            var producerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDonationEventProducer));
            if (producerDescriptor != null)
            {
                services.Remove(producerDescriptor);
            }
            services.AddSingleton<IDonationEventProducer>(TestEventProducer);

            // 3. Remove Background Hosted Services to prevent background race conditions during tests
            var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var hs in hostedServices)
            {
                services.Remove(hs);
            }
        });
    }

    private void EnsureDatabaseCreated()
    {
        try
        {
            var masterConnStr = IntegrationTestConfig.GetMasterConnectionString();
            using var conn = new NpgsqlConnection(masterConnStr);
            conn.Open();

            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{IntegrationTestConfig.DefaultDatabaseName}'";
            var exists = checkCmd.ExecuteScalar() != null;

            if (!exists)
            {
                using var createCmd = conn.CreateCommand();
                createCmd.CommandText = $"CREATE DATABASE \"{IntegrationTestConfig.DefaultDatabaseName}\"";
                createCmd.ExecuteNonQuery();
            }
        }
        catch
        {
            // Database may already exist or user may have created it manually
        }
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
        
        // Remove all test donation records
        var allDonations = await db.Donations.ToListAsync();
        if (allDonations.Count > 0)
        {
            db.Donations.RemoveRange(allDonations);
            await db.SaveChangesAsync();
        }

        TestEventProducer.Clear();
    }
}
