using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using DeliveryWorkflowService.Data;

namespace DeliveryWorkflowService.UnitTests.Helpers;

public static class TestDbContextFactory
{
    public static DeliveryDbContext CreateDbContext(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DeliveryDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new DeliveryDbContext(options);
        context.Database.EnsureCreated();

        // Ensure Requests table exists for raw SQL execution in DeliveriesController
        using var cmd = connection.CreateCommand();
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

        return context;
    }

    public static void SeedRequest(
        DeliveryDbContext context,
        int requestId,
        int donationId,
        string donorId,
        string orgId,
        string status = "ACCEPTED",
        string? title = "Surplus Fresh Apples",
        string? orgName = "City Hope Food Bank")
    {
        var conn = context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO ""Requests"" (""Id"", ""DonationId"", ""DonationTitle"", ""OrganizationId"", ""OrganizationName"", ""DonorId"", ""Status"")
            VALUES (@id, @donationId, @title, @orgId, @orgName, @donorId, @status);
        ";

        AddParam(cmd, "@id", requestId);
        AddParam(cmd, "@donationId", donationId);
        AddParam(cmd, "@title", (object?)title ?? DBNull.Value);
        AddParam(cmd, "@orgId", (object?)orgId ?? DBNull.Value);
        AddParam(cmd, "@orgName", (object?)orgName ?? DBNull.Value);
        AddParam(cmd, "@donorId", (object?)donorId ?? DBNull.Value);
        AddParam(cmd, "@status", (object?)status ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    private static void AddParam(IDbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
