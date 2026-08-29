using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using UserService.Data;
using UserService.DTOs;
using UserService.Models;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly RescuePlateDbContext _dbContext;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<RescuePlateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new RescuePlateDbContext(options);
        _tokenServiceMock = new Mock<ITokenService>();

        _tokenServiceMock
            .Setup(t => t.GenerateToken(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns("mocked-jwt-token-xyz");

        _authService = new AuthService(_dbContext, _tokenServiceMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task RegisterAsync_ValidDonor_CreatesUserAndDonorProfile()
    {
        // Arrange (TC-AUTH-01)
        var dto = new RegisterDto
        {
            Email = "donor@bistro.com",
            Password = "SecurePassword123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Bistro Delight",
            ContactName = "John Chef",
            Phone = "+1-555-0199",
            Location = "123 Main Street",
            DonorType = "Restaurant"
        };

        // Act
        var (success, message, data) = await _authService.RegisterAsync(dto);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Registration successful!");
        data.Should().NotBeNull();
        data!.Email.Should().Be("donor@bistro.com");
        data.Role.Should().Be("DONOR");
        data.BusinessName.Should().Be("Bistro Delight");
        data.Token.Should().Be("mocked-jwt-token-xyz");

        var userInDb = await _dbContext.Users.Include(u => u.DonorProfile).FirstOrDefaultAsync(u => u.Email == "donor@bistro.com");
        userInDb.Should().NotBeNull();
        userInDb!.IsActive.Should().BeTrue();
        userInDb.DonorProfile.Should().NotBeNull();
        userInDb.DonorProfile!.BusinessName.Should().Be("Bistro Delight");
        userInDb.DonorProfile.DonorType.Should().Be("Restaurant");
        userInDb.DonorProfile.Address.Should().Be("123 Main Street");
        userInDb.DonorProfile.Phone.Should().Be("+1-555-0199");

        // TC-AUTH-05: Password Hashing Verification
        userInDb.PasswordHash.Should().NotBe("SecurePassword123!");
        BCrypt.Net.BCrypt.Verify("SecurePassword123!", userInDb.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_ValidOrganization_CreatesUserAndOrganizationProfile()
    {
        // Arrange (TC-AUTH-02)
        var dto = new RegisterDto
        {
            Email = "contact@hopecommunity.org",
            Password = "CharityPassword123!",
            Role = UserRole.ORGANIZATION,
            BusinessOrOrgName = "Hope Community Food Bank",
            ContactName = "Sister Maria",
            Phone = "+1-555-0244",
            Location = "789 Hope Way",
            AcceptedFoodTypes = new List<string> { "Cooked Meals", "Bakery", "Fresh Produce" }
        };

        // Act
        var (success, message, data) = await _authService.RegisterAsync(dto);

        // Assert
        success.Should().BeTrue();
        data.Should().NotBeNull();
        data!.Role.Should().Be("ORGANIZATION");

        var userInDb = await _dbContext.Users.Include(u => u.OrganizationProfile).FirstOrDefaultAsync(u => u.Email == "contact@hopecommunity.org");
        userInDb.Should().NotBeNull();
        userInDb!.OrganizationProfile.Should().NotBeNull();
        userInDb.OrganizationProfile!.OrganizationName.Should().Be("Hope Community Food Bank");
        userInDb.OrganizationProfile.AcceptedFoodCategories.Should().Be("Cooked Meals;Bakery;Fresh Produce");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailureWithConflictMessage()
    {
        // Arrange (TC-AUTH-03)
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@rescueplate.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass123!"),
            Role = UserRole.DONOR,
            IsActive = true
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var duplicateDto = new RegisterDto
        {
            Email = "EXISTING@rescueplate.org", // Case insensitive check
            Password = "AnotherPassword123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Another Business",
            Location = "Downtown"
        };

        // Act
        var (success, message, data) = await _authService.RegisterAsync(duplicateDto);

        // Assert
        success.Should().BeFalse();
        message.Should().Contain("already exists");
        data.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessAndAuthResponse()
    {
        // Arrange (TC-AUTH-06)
        var password = "CorrectPassword123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "active_donor@food.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11),
            Role = UserRole.DONOR,
            IsActive = true
        };
        var profile = new DonorProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            BusinessName = "Grand Bakery",
            ContactPerson = "Paul Baker",
            Address = "45 Elm St"
        };
        _dbContext.Users.Add(user);
        _dbContext.DonorProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "active_donor@food.com",
            Password = password
        };

        // Act
        var (success, message, data) = await _authService.LoginAsync(loginDto);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Signed in successfully!");
        data.Should().NotBeNull();
        data!.UserId.Should().Be(user.Id);
        data.Email.Should().Be(user.Email);
        data.Role.Should().Be("DONOR");
        data.BusinessName.Should().Be("Grand Bakery");
        data.Name.Should().Be("Paul Baker");
        data.Token.Should().Be("mocked-jwt-token-xyz");
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ReturnsFailure()
    {
        // Arrange (TC-AUTH-07)
        var loginDto = new LoginDto
        {
            Email = "nonexistent@user.com",
            Password = "SomePassword123"
        };

        // Act
        var (success, message, data) = await _authService.LoginAsync(loginDto);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Invalid email address or password.");
        data.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange (TC-AUTH-08)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "validuser@rescue.org",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("RealPassword123!"),
            Role = UserRole.ORGANIZATION,
            IsActive = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "validuser@rescue.org",
            Password = "WrongPassword999!"
        };

        // Act
        var (success, message, data) = await _authService.LoginAsync(loginDto);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Invalid email address or password.");
        data.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_DeactivatedAccount_ReturnsDeactivatedMessage()
    {
        // Arrange (TC-AUTH-09)
        var password = "Pass123456!";
        var deactivatedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "banned@user.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.DONOR,
            IsActive = false
        };
        _dbContext.Users.Add(deactivatedUser);
        await _dbContext.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = "banned@user.com",
            Password = password
        };

        // Act
        var (success, message, data) = await _authService.LoginAsync(loginDto);

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Your account has been deactivated by the system administrator.");
        data.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAccountAsync_CorrectPassword_RemovesUserFromDatabase()
    {
        // Arrange (TC-AUTH-14)
        var password = "DeleteMe123!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "delete_target@domain.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.DONOR,
            IsActive = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var (success, message) = await _authService.DeleteAccountAsync(user.Id, password);

        // Assert
        success.Should().BeTrue();
        message.Should().Be("Account has been permanently deleted.");
        (await _dbContext.Users.FindAsync(user.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAccountAsync_IncorrectPassword_AbortsDeletion()
    {
        // Arrange (TC-AUTH-15)
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "safe_user@domain.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ActualSecretPassword"),
            Role = UserRole.DONOR,
            IsActive = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var (success, message) = await _authService.DeleteAccountAsync(user.Id, "WrongSecret");

        // Assert
        success.Should().BeFalse();
        message.Should().Be("Incorrect password. Account deletion aborted.");
        (await _dbContext.Users.FindAsync(user.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAccountAsync_NonExistentUser_ReturnsNotFound()
    {
        // Arrange (TC-AUTH-16)
        var nonExistentId = Guid.NewGuid();

        // Act
        var (success, message) = await _authService.DeleteAccountAsync(nonExistentId, "AnyPassword");

        // Assert
        success.Should().BeFalse();
        message.Should().Be("User account not found.");
    }
}
