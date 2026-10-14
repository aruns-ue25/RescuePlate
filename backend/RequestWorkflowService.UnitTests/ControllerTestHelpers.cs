using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using RequestWorkflowService.Controllers;
using RequestWorkflowService.Data;
using RequestWorkflowService.Kafka;

namespace RequestWorkflowService.UnitTests;

public static class ControllerTestHelpers
{
    public static RequestDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<RequestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new RequestDbContext(options);
    }

    public static IHttpClientFactory CreateStubHttpClientFactory(HttpStatusCode statusCode, object? responseContent)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var httpResponse = new HttpResponseMessage(statusCode);
        if (responseContent != null)
        {
            httpResponse.Content = JsonContent.Create(responseContent);
        }
        else
        {
            httpResponse.Content = new StringContent(string.Empty);
        }

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5002")
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return factoryMock.Object;
    }

    public static RequestsController CreateRequestsController(
        RequestDbContext dbContext,
        Mock<IRequestEventProducer>? eventProducerMock = null,
        IHttpClientFactory? httpClientFactory = null,
        string callerId = "org_101",
        string callerName = "Hope Relief Org",
        string callerRole = "ORGANIZATION")
    {
        eventProducerMock ??= new Mock<IRequestEventProducer>();
        httpClientFactory ??= CreateStubHttpClientFactory(HttpStatusCode.OK, new
        {
            success = true,
            data = new
            {
                id = 1,
                foodTitle = "Fresh Sandwiches",
                donorId = "donor_202",
                totalQuantity = 20,
                remainingQuantity = 15,
                unit = "packs",
                status = "Available",
                expiryTime = DateTime.UtcNow.AddHours(5).ToString("O")
            }
        });

        var configDict = new Dictionary<string, string?>
        {
            { "Kafka:RequestTopic", "test-request-events" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
        var loggerMock = new Mock<ILogger<RequestsController>>();

        var controller = new RequestsController(
            dbContext,
            httpClientFactory,
            eventProducerMock.Object,
            configuration,
            loggerMock.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, callerId),
            new(ClaimTypes.Name, callerName),
            new(ClaimTypes.Role, callerRole),
            new("BusinessName", callerName)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        return controller;
    }

    public static OrgNeedRequestsController CreateOrgNeedRequestsController(
        RequestDbContext dbContext,
        Mock<IRequestEventProducer>? eventProducerMock = null,
        string callerId = "org_101",
        string callerName = "Hope Relief Org",
        string callerRole = "ORGANIZATION")
    {
        eventProducerMock ??= new Mock<IRequestEventProducer>();
        var configDict = new Dictionary<string, string?>
        {
            { "Kafka:NeedRequestTopic", "test-need-events" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
        var loggerMock = new Mock<ILogger<OrgNeedRequestsController>>();

        var controller = new OrgNeedRequestsController(
            dbContext,
            eventProducerMock.Object,
            configuration,
            loggerMock.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, callerId),
            new(ClaimTypes.Name, callerName),
            new(ClaimTypes.Role, callerRole),
            new("BusinessName", callerName)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        return controller;
    }

    public static NotificationsController CreateNotificationsController(
        RequestDbContext dbContext,
        string callerId = "user_101")
    {
        var loggerMock = new Mock<ILogger<NotificationsController>>();
        var controller = new NotificationsController(dbContext, loggerMock.Object);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, callerId),
            new("sub", callerId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        return controller;
    }
}
