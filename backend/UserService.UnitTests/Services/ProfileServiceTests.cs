using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests.Services;

public class ProfileServiceTests : IDisposable
{
    private readonly RescuePlateDbContext _dbContext;
    private readonly ProfileService _profileService;

    public ProfileServiceTests()
    {
        var options = new DbContextOptionsBuilder<RescuePlateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RescuePlateDbContext(options);
        _profileService = new ProfileService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetProfileByUserIdAsync_DonorUser_ReturnsPopulatedDonorProfileDto()
    {
        // Arrange (TC-PROF-01)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "donor@bistro.com",
            Role = UserRole.DONOR,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-1)
        };
        var profile = new DonorProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BusinessName = "Sunset Bistro",
            ContactPerson = "Chef Alex",
            DonorType = "Restaurant",
            Address = "100 Coastal Hwy",
            Phone = "+1-555-4000",
            Bio = "Artisan bistro serving fresh food."
        };
        _dbContext.Users.Add(user);
        _dbContext.DonorProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _profileService.GetProfileByUserIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.Id);
        result.Email.Should().Be("donor@bistro.com");
        result.Role.Should().Be("DONOR");
        result.BusinessOrOrgName.Should().Be("Sunset Bistro");
        result.ContactName.Should().Be("Chef Alex");
        result.DonorType.Should().Be("Restaurant");
        result.Address.Should().Be("100 Coastal Hwy");
        result.Phone.Should().Be("+1-555-4000");
        result.BioOrDescription.Should().Be("Artisan bistro serving fresh food.");
    }

    [Fact]
    public async Task GetProfileByUserIdAsync_OrganizationUser_ReturnsPopulatedOrgProfileDtoWithSplitCategories()
    {
        // Arrange (TC-PROF-02)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "charity@shelter.org",
            Role = UserRole.ORGANIZATION,
            IsActive = true
        };
        var profile = new OrganizationProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationName = "City Hope Mission",
            ContactPerson = "Director Dave",
            Address = "200 Market St",
            Phone = "+1-555-6000",
            Description = "Serving the homeless community.",
            RegistrationNumber = "NGO-8876",
            AcceptedFoodCategories = "Cooked Meals;Bakery;Dairy & Chilled"
        };
        _dbContext.Users.Add(user);
        _dbContext.OrganizationProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _profileService.GetProfileByUserIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.Id);
        result.Role.Should().Be("ORGANIZATION");
        result.BusinessOrOrgName.Should().Be("City Hope Mission");
        result.RegistrationNumber.Should().Be("NGO-8876");
        result.AcceptedFoodCategories.Should().ContainInOrder("Cooked Meals", "Bakery", "Dairy & Chilled");
    }

    [Fact]
    public async Task GetProfileByUserIdAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange (TC-PROF-04)
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _profileService.GetProfileByUserIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProfileAsync_DonorUser_UpdatesFieldsAndTimestamps()
    {
        // Arrange (TC-PROF-05)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "update_donor@test.com",
            Role = UserRole.DONOR,
            IsActive = true
        };
        var initialProfile = new DonorProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BusinessName = "Old Name",
            Address = "Old Address",
            DonorType = "Bakery"
        };
        _dbContext.Users.Add(user);
        _dbContext.DonorProfiles.Add(initialProfile);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateProfileDto
        {
            BusinessOrOrgName = "Updated Artisan Bakery",
            ContactName = "Master Baker",
            Address = "500 New Street",
            Phone = "+1-555-9999",
            BioOrDescription = "Fresh organic breads daily",
            DonorType = "Supermarket"
        };

        // Act
        var (success, message, data) = await _profileService.UpdateProfileAsync(user.Id, updateDto);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Profile updated successfully!");
        data.Should().NotBeNull();
        data!.BusinessOrOrgName.Should().Be("Updated Artisan Bakery");
        data.ContactName.Should().Be("Master Baker");
        data.Address.Should().Be("500 New Street");
        data.DonorType.Should().Be("Supermarket");

        var updatedEntity = await _dbContext.DonorProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.BusinessName.Should().Be("Updated Artisan Bakery");
        updatedEntity.DonorType.Should().Be("Supermarket");
    }

    [Fact]
    public async Task UpdateProfileAsync_OrganizationUser_UpdatesCategoriesList()
    {
        // Arrange (TC-PROF-06)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "org_update@shelter.org",
            Role = UserRole.ORGANIZATION,
            IsActive = true
        };
        var initialProfile = new OrganizationProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationName = "Old Org Name",
            Address = "Old Address",
            AcceptedFoodCategories = "Bakery"
        };
        _dbContext.Users.Add(user);
        _dbContext.OrganizationProfiles.Add(initialProfile);
        await _dbContext.SaveChangesAsync();

        var updateDto = new UpdateProfileDto
        {
            BusinessOrOrgName = "New Hope Food Bank",
            AcceptedFoodCategories = new List<string> { "Cooked Meals", "Dairy & Chilled", "Packaged Dry" }
        };

        // Act
        var (success, message, data) = await _profileService.UpdateProfileAsync(user.Id, updateDto);

        // Assert
        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.AcceptedFoodCategories.Should().ContainInOrder("Cooked Meals", "Dairy & Chilled", "Packaged Dry");

        var updatedEntity = await _dbContext.OrganizationProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.AcceptedFoodCategories.Should().Be("Cooked Meals;Dairy & Chilled;Packaged Dry");
    }

    [Fact]
    public async Task UpdateProfileAsync_NonExistentUser_ReturnsFailure()
    {
        // Arrange (TC-PROF-07)
        var nonExistentId = Guid.NewGuid();
        var updateDto = new UpdateProfileDto { BusinessOrOrgName = "Test" };

        // Act
        var (success, message, data) = await _profileService.UpdateProfileAsync(nonExistentId, updateDto);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("User not found.");
        data.Should().BeNull();
    }
}
