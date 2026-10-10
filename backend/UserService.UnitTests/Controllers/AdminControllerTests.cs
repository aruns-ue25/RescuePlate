using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using UserService.Controllers;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests.Controllers;

public class AdminControllerTests : IDisposable
{
    private readonly RescuePlateDbContext _dbContext;
    private readonly Mock<IAdminService> _adminServiceMock;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        var options = new DbContextOptionsBuilder<RescuePlateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RescuePlateDbContext(options);
        _adminServiceMock = new Mock<IAdminService>();
        _controller = new AdminController(_dbContext, _adminServiceMock.Object);

        SetUserContext(Guid.NewGuid());
    }

    private void SetUserContext(Guid userId, string email = "admin@rescueplate.org", string role = "ADMIN")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAllRegisteredUsersExcludingPasswordHashes()
    {
        // Arrange (A8)
        var donorUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "donor@restaurant.com",
            PasswordHash = "SuperSecretPasswordHash",
            Role = UserRole.DONOR,
            IsActive = true
        };
        var donorProfile = new DonorProfile
        {
            Id = Guid.NewGuid(),
            UserId = donorUser.Id,
            BusinessName = "Green Leaf",
            Address = "100 Ave"
        };

        _dbContext.Users.Add(donorUser);
        _dbContext.DonorProfiles.Add(donorProfile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        json.Should().NotContain("PasswordHash");
        json.Should().NotContain("SuperSecretPasswordHash");
    }

    [Fact]
    public async Task ToggleUserStatus_ExistingUser_UpdatesActiveStatus()
    {
        // Arrange (A8)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "target@user.com",
            Role = UserRole.DONOR,
            IsActive = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _adminServiceMock
            .Setup(s => s.UpdateUserStatusAsync(user.Id, false, It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((true, "User account has been deactivated."));

        var statusDto = new UserStatusUpdateDto { IsActive = false };

        // Act
        var result = await _controller.ToggleUserStatus(user.Id, statusDto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ToggleUserStatus_AdminUser_ReturnsBadRequest()
    {
        // Arrange (A9)
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@rescueplate.org",
            Role = UserRole.ADMIN,
            IsActive = true
        };
        _dbContext.Users.Add(adminUser);
        await _dbContext.SaveChangesAsync();

        _adminServiceMock
            .Setup(s => s.UpdateUserStatusAsync(adminUser.Id, false, It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, "Administrator account status cannot be modified."));

        var statusDto = new UserStatusUpdateDto { IsActive = false };

        // Act
        var result = await _controller.ToggleUserStatus(adminUser.Id, statusDto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ToggleUserStatus_NonExistentUser_Returns404NotFound()
    {
        // Arrange (A8)
        var nonExistentId = Guid.NewGuid();
        var statusDto = new UserStatusUpdateDto { IsActive = false };

        _adminServiceMock
            .Setup(s => s.UpdateUserStatusAsync(nonExistentId, false, It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((false, "User not found."));

        // Act
        var result = await _controller.ToggleUserStatus(nonExistentId, statusDto);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }
}
