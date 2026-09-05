using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;
using Xunit;

namespace UserService.UnitTests.Data;

public class RescuePlateDbContextTests : IDisposable
{
    private readonly RescuePlateDbContext _dbContext;

    public RescuePlateDbContextTests()
    {
        var options = new DbContextOptionsBuilder<RescuePlateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RescuePlateDbContext(options);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task DeleteUser_CascadesDeleteToDonorProfile()
    {
        // Arrange (TC-DB-02)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "donor_cascade@test.com",
            PasswordHash = "hash",
            Role = UserRole.DONOR
        };
        var profile = new DonorProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BusinessName = "Cascade Bakery"
        };
        _dbContext.Users.Add(user);
        _dbContext.DonorProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        // Assert
        (await _dbContext.DonorProfiles.FindAsync(profile.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteUser_CascadesDeleteToOrganizationProfile()
    {
        // Arrange (TC-DB-03)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "org_cascade@test.com",
            PasswordHash = "hash",
            Role = UserRole.ORGANIZATION
        };
        var profile = new OrganizationProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationName = "Cascade Charity"
        };
        _dbContext.Users.Add(user);
        _dbContext.OrganizationProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        // Assert
        (await _dbContext.OrganizationProfiles.FindAsync(profile.Id)).Should().BeNull();
    }

    [Fact]
    public void ModelConfiguration_DefinesUniqueEmailIndex()
    {
        // Arrange (TC-DB-01)
        var entityType = _dbContext.Model.FindEntityType(typeof(User));
        entityType.Should().NotBeNull();

        var emailIndex = entityType!.FindIndex(entityType.FindProperty(nameof(User.Email))!);
        emailIndex.Should().NotBeNull();
        emailIndex!.IsUnique.Should().BeTrue();
    }
}
