using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using DonationService.Data;
using DonationService.Kafka;
using DonationService.Services;

namespace DonationService.UnitTests;

public static class TestHelpers
{
    public static DonationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DonationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new DonationDbContext(options);
    }

    public static IHttpClientFactory CreateMockHttpClientFactory(string? address = "123 Test Street, Colombo", HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var jsonResponse = JsonSerializer.Serialize(new
        {
            success = statusCode == HttpStatusCode.OK,
            data = new
            {
                userId = "donor-1",
                role = "DONOR",
                businessName = "Test Donor Business",
                address = address,
                bioOrDescription = "Test bio",
                donorType = "Restaurant",
                profilePictureUrl = "http://example.com/pic.jpg",
                acceptedFoodCategories = new List<string> { "Cooked Meals", "Bakery" }
            }
        });

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return factoryMock.Object;
    }

    public static IConfiguration CreateMockConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["UserService:BaseUrl"] = "http://localhost:5000",
            ["Kafka:BootstrapServers"] = "localhost:9092",
            ["Kafka:Topic"] = "donation-events"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    public static (DonationServiceImpl service, DonationDbContext dbContext, Mock<IDonationEventProducer> eventProducerMock) CreateDonationService(
        DonationDbContext? dbContext = null,
        IHttpClientFactory? httpClientFactory = null)
    {
        var db = dbContext ?? CreateInMemoryDbContext();
        var logger = NullLogger<DonationServiceImpl>.Instance;
        var httpFactory = httpClientFactory ?? CreateMockHttpClientFactory();
        var config = CreateMockConfiguration();
        var eventProducerMock = new Mock<IDonationEventProducer>();
        eventProducerMock
            .Setup(p => p.PublishEventAsync(It.IsAny<Events.DonationEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new DonationServiceImpl(db, logger, httpFactory, config, eventProducerMock.Object);

        return (service, db, eventProducerMock);
    }
}
