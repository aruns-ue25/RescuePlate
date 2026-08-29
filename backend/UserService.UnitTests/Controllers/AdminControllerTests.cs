using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Controllers;
using UserService.Data;
using UserService.Models;
using Xunit;

namespace UserService.UnitTests.Controllers;

public class AdminControllerTests : IDisposable
{
    private readonly RescuePlateDbContext _dbContext;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        var options = new DbContextOptionsBuilder<RescuePlateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RescuePlateDbContext(options);
        _controller = new AdminController(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAllRegisteredUsersWithProfiles()
    {
        // Arrange (TC-ADM-01)
        var donorUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "donor@restaurant.com",
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

        var orgUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "charity@shelter.org",
            Role = UserRole.ORGANIZATION,
            IsActive = true
        };
        var orgProfile = new OrganizationProfile
        {
            Id = Guid.NewGuid(),
            UserId = orgUser.Id,
            OrganizationName = "Hope Mission",
            Address = "200 St"
        };

        _dbContext.Users.AddRange(donorUser, orgUser);
        _dbContext.DonorProfiles.Add(donorProfile);
        _dbContext.OrganizationProfiles.Add(orgProfile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ToggleUserStatus_ExistingUser_UpdatesActiveStatus()
    {
        // Arrange (TC-ADM-04)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "target@user.com",
            Role = UserRole.DONOR,
            IsActive = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var statusDto = new StatusUpdateDto { IsActive = false };

        // Act
        var result = await _controller.ToggleUserStatus(user.Id, statusDto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        updatedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleUserStatus_NonExistentUser_Returns404NotFound()
    {
        // Arrange (TC-ADM-05)
        var nonExistentId = Guid.NewGuid();
        var statusDto = new StatusUpdateDto { IsActive = false };

        // Act
        var result = await _controller.ToggleUserStatus(nonExistentId, statusDto);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }
}
