using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DeliveryWorkflowService.Data;

namespace DeliveryWorkflowService.IntegrationTests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _sqliteConnection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _sqliteConnection = new SqliteConnection("DataSource=:memory:");
        _sqliteConnection.Open();

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = AuthTestHelper.SecretKey,
                ["Jwt:Issuer"] = AuthTestHelper.Issuer,
                ["Jwt:Audience"] = AuthTestHelper.Audience
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core internal descriptors from Program.cs to allow clean provider replacement
            var efDescriptors = services.Where(d =>
                (d.ServiceType.Namespace != null && d.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore")) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DbContextOptions<DeliveryDbContext>) ||
                d.ServiceType == typeof(DeliveryDbContext)).ToList();

            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<DeliveryDbContext>(options =>
            {
                options.UseSqlite(_sqliteConnection);
            });

            // Remove hosted services if any
            var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var hs in hostedServices)
            {
                services.Remove(hs);
            }
        });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        db.Database.EnsureCreated();

        using var cmd = _sqliteConnection!.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ""Requests"" (
                ""Id"" INTEGER PRIMARY KEY,
                ""DonationId"" INTEGER NOT NULL,
                ""DonationTitle"" TEXT,
                ""OrganizationId"" TEXT,
                ""OrganizationName"" TEXT,
                ""DonorId"" TEXT,
                ""Status"" TEXT
            );
        ";
        cmd.ExecuteNonQuery();
    }

    public void SeedRequest(int reqId, int donationId, string donorId, string orgId, string status = "ACCEPTED", string title = "Surplus Apples", string orgName = "Hope Shelter")
    {
        using var cmd = _sqliteConnection!.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO ""Requests"" (""Id"", ""DonationId"", ""DonationTitle"", ""OrganizationId"", ""OrganizationName"", ""DonorId"", ""Status"")
            VALUES (@id, @donationId, @title, @orgId, @orgName, @donorId, @status);
        ";

        AddParam(cmd, "@id", reqId);
        AddParam(cmd, "@donationId", donationId);
        AddParam(cmd, "@title", title);
        AddParam(cmd, "@orgId", orgId);
        AddParam(cmd, "@orgName", orgName);
        AddParam(cmd, "@donorId", donorId);
        AddParam(cmd, "@status", status);

        cmd.ExecuteNonQuery();
    }

    private static void AddParam(IDbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();

        db.DeliveryNotifications.RemoveRange(db.DeliveryNotifications);
        db.DeliveryStatusHistories.RemoveRange(db.DeliveryStatusHistories);
        db.DeliveryArrangements.RemoveRange(db.DeliveryArrangements);
        await db.SaveChangesAsync();

        using var cmd = _sqliteConnection!.CreateCommand();
        cmd.CommandText = @"DELETE FROM ""Requests"";";
        cmd.ExecuteNonQuery();
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
