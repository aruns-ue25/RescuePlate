namespace DonationService.IntegrationTests.Helpers;

public static class IntegrationTestConfig
{
    public const string DefaultDatabaseName = "RescuePlate_Donation_IntegrationTestDB";
    public const string DefaultHost = "localhost";
    public const string DefaultPort = "5432";
    public const string DefaultUser = "postgres";
    public const string DefaultPassword = "postgres";

    public static string GetConnectionString()
    {
        var explicitConnStr = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(explicitConnStr))
        {
            return explicitConnStr;
        }

        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? DefaultHost;
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? DefaultPort;
        var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? DefaultDatabaseName;
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? DefaultUser;
        var pass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? DefaultPassword;

        return $"Host={host};Port={port};Database={dbName};Username={user};Password={pass}";
    }

    public static string GetMasterConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? DefaultHost;
        var port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? DefaultPort;
        var user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? DefaultUser;
        var pass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? DefaultPassword;

        return $"Host={host};Port={port};Database=postgres;Username={user};Password={pass}";
    }
}
