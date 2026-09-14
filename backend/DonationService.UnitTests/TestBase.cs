using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Http;
using DonationService.Data;
using DonationService.Services;

namespace DonationService.UnitTests;

public class TestBase
{
    protected DonationDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<DonationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var context = new DonationDbContext(options);
        return context;
    }

    protected DonationServiceImpl CreateService(DonationDbContext context)
    {
        var loggerMock = new Mock<ILogger<DonationServiceImpl>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var configurationMock = new Mock<IConfiguration>();
        return new DonationServiceImpl(context, loggerMock.Object, httpClientFactoryMock.Object, configurationMock.Object);
    }
}
