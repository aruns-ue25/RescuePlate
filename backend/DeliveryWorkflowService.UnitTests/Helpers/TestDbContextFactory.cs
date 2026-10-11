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
            CREATE TABLE IF NOT EXISTS ""OrgNeedRequests"" (
                ""Id"" INTEGER PRIMARY KEY,
                ""Title"" TEXT,
                ""OrganizationId"" TEXT,
                ""OrganizationName"" TEXT,
                ""Status"" TEXT
            );
            CREATE TABLE IF NOT EXISTS ""DonorOffers"" (
                ""Id"" INTEGER PRIMARY KEY,
                ""OrgFoodNeedRequestId"" INTEGER NOT NULL,
                ""OrgFoodNeedRequestTitle"" TEXT,
                ""DonorId"" TEXT,
                ""DonorName"" TEXT,
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

    public static void SeedFoodOffer(
        DeliveryDbContext context,
        int offerId,
        int orgFoodNeedRequestId,
        string donorId,
        string orgId,
        string status = "ACCEPTED",
        string? needTitle = "Fresh Bakery Buns for Shelter",
        string? donorName = "Bakery Donor",
        string? orgName = "Community Shelter")
    {
        var conn = context.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR REPLACE INTO ""OrgNeedRequests"" (""Id"", ""Title"", ""OrganizationId"", ""OrganizationName"", ""Status"")
                VALUES (@needId, @needTitle, @orgId, @orgName, 'OPEN');
            ";
            AddParam(cmd, "@needId", orgFoodNeedRequestId);
            AddParam(cmd, "@needTitle", (object?)needTitle ?? DBNull.Value);
            AddParam(cmd, "@orgId", (object?)orgId ?? DBNull.Value);
            AddParam(cmd, "@orgName", (object?)orgName ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT OR REPLACE INTO ""DonorOffers"" (""Id"", ""OrgFoodNeedRequestId"", ""OrgFoodNeedRequestTitle"", ""DonorId"", ""DonorName"", ""Status"")
                VALUES (@offerId, @needId, @needTitle, @donorId, @donorName, @status);
            ";
            AddParam(cmd, "@offerId", offerId);
            AddParam(cmd, "@needId", orgFoodNeedRequestId);
            AddParam(cmd, "@needTitle", (object?)needTitle ?? DBNull.Value);
            AddParam(cmd, "@donorId", (object?)donorId ?? DBNull.Value);
            AddParam(cmd, "@donorName", (object?)donorName ?? DBNull.Value);
            AddParam(cmd, "@status", (object?)status ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    private static void AddParam(IDbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }
}
