using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Controllers;
using UserService.DTOs;
using UserService.Models;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    private void SetUserContext(Guid userId, string role = "DONOR")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task Register_ValidDto_Returns201Created()
    {
        // Arrange (TC-AUTH-01)
        var dto = new RegisterDto
        {
            Email = "newuser@test.com",
            Password = "Password123!",
            Role = UserRole.DONOR,
            BusinessOrOrgName = "Green Leaf",
            Location = "123 Main"
        };
        var authResponse = new AuthResponseDto
        {
            UserId = Guid.NewGuid(),
            Email = dto.Email,
            Role = "DONOR",
            Token = "valid-token"
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(dto))
            .ReturnsAsync((true, "Registration successful!", authResponse));

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Conflict()
    {
        // Arrange (TC-AUTH-03)
        var dto = new RegisterDto { Email = "duplicate@test.com", Password = "Password123!" };
        _authServiceMock
            .Setup(s => s.RegisterAsync(dto))
            .ReturnsAsync((false, "An account with this email address already exists.", null));

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var conflictResult = result as ConflictObjectResult;
        conflictResult.Should().NotBeNull();
        conflictResult!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Register_InvalidModelState_Returns400BadRequest()
    {
        // Arrange (TC-AUTH-04)
        _controller.ModelState.AddModelError("Email", "Email is required");
        var dto = new RegisterDto();

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200Ok()
    {
        // Arrange (TC-AUTH-06)
        var dto = new LoginDto { Email = "user@test.com", Password = "Password123!" };
        var authResponse = new AuthResponseDto { Email = dto.Email, Token = "jwt-token" };

        _authServiceMock
            .Setup(s => s.LoginAsync(dto))
            .ReturnsAsync((true, "Signed in successfully!", authResponse));

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401Unauthorized()
    {
        // Arrange (TC-AUTH-07 / 08)
        var dto = new LoginDto { Email = "user@test.com", Password = "WrongPassword" };
        _authServiceMock
            .Setup(s => s.LoginAsync(dto))
            .ReturnsAsync((false, "Invalid email address or password.", null));

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Login_InvalidModelState_Returns400BadRequest()
    {
        // Arrange (TC-AUTH-10)
        _controller.ModelState.AddModelError("Password", "Password is required");
        var dto = new LoginDto();

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public void Logout_AuthenticatedSession_Returns200Ok()
    {
        // Arrange (TC-AUTH-12)
        SetUserContext(Guid.NewGuid());

        // Act
        var result = _controller.Logout();

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteAccount_ValidPassword_Returns200Ok()
    {
        // Arrange (TC-AUTH-14)
        var userId = Guid.NewGuid();
        SetUserContext(userId);

        var dto = new DeleteAccountDto { Password = "CorrectPassword" };
        _authServiceMock
            .Setup(s => s.DeleteAccountAsync(userId, "CorrectPassword"))
            .ReturnsAsync((true, "Account has been permanently deleted."));

        // Act
        var result = await _controller.DeleteAccount(dto);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeleteAccount_IncorrectPassword_Returns400BadRequest()
    {
        // Arrange (TC-AUTH-15)
        var userId = Guid.NewGuid();
        SetUserContext(userId);

        var dto = new DeleteAccountDto { Password = "WrongPassword" };
        _authServiceMock
            .Setup(s => s.DeleteAccountAsync(userId, "WrongPassword"))
            .ReturnsAsync((false, "Incorrect password. Account deletion aborted."));

        // Act
        var result = await _controller.DeleteAccount(dto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task DeleteAccount_MissingClaims_Returns401Unauthorized()
    {
        // Arrange (TC-AUTH-16)
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        var dto = new DeleteAccountDto { Password = "AnyPassword" };

        // Act
        var result = await _controller.DeleteAccount(dto);

        // Assert
        var unauthorizedResult = result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);
    }
}
