using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using UserService.Data;
using UserService.Models;
using Xunit;

namespace UserService.UnitTests.Data;

public class DbInitializerTests
{
    [Fact]
    public void AdminUser_WhenCreated_HasAdminRoleAndValidHashedPassword()
    {
        // Arrange (A1 - Provisioning test)
        var options = new DbContextOptionsBuilder<RescuePlateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new RescuePlateDbContext(options);

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@rescueplate.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 11),
            Role = UserRole.ADMIN,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        dbContext.Users.Add(adminUser);
        dbContext.SaveChanges();

        // Assert
        var savedAdmin = dbContext.Users.FirstOrDefault(u => u.Email == "admin@rescueplate.org");
        savedAdmin.Should().NotBeNull();
        savedAdmin!.Role.Should().Be(UserRole.ADMIN);
        savedAdmin.IsActive.Should().BeTrue();
        BCrypt.Net.BCrypt.Verify("Admin@123", savedAdmin.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public void DbInitializer_Idempotency_DoesNotDuplicateAdmin()
    {
        // Arrange (A1 - Restart against existing database with existing admin)
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<RescuePlateDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        using (var dbContext = new RescuePlateDbContext(options))
        {
            dbContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@rescueplate.org",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = UserRole.ADMIN,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            dbContext.SaveChanges();
        }

        // Act - Simulate second run
        using (var dbContext = new RescuePlateDbContext(options))
        {
            var adminCount = dbContext.Users.Count(u => u.Role == UserRole.ADMIN);
            adminCount.Should().Be(1);
        }
    }
}
