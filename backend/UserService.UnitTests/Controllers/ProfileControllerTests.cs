using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Controllers;
using UserService.DTOs;
using UserService.Services;
using Xunit;

namespace UserService.UnitTests.Controllers;

public class ProfileControllerTests
{
    private readonly Mock<IProfileService> _profileServiceMock;
    private readonly Mock<IWebHostEnvironment> _environmentMock;
    private readonly ProfileController _controller;

    public ProfileControllerTests()
    {
        _profileServiceMock = new Mock<IProfileService>();

        _environmentMock = new Mock<IWebHostEnvironment>();

        _environmentMock
            .Setup(e => e.WebRootPath)
            .Returns(Path.Combine(Path.GetTempPath(), "RescuePlateTest"));

        _controller = new ProfileController(
            _profileServiceMock.Object,
            _environmentMock.Object
        );
    }

    private void SetUserContext(Guid userId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, "DONOR")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");

        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            }
        };
    }

    [Fact]
    public async Task GetMyProfile_AuthenticatedUser_Returns200OkWithProfile()
    {
        // Arrange (TC-PROF-01)
        var userId = Guid.NewGuid();

        SetUserContext(userId);

        var profileDto = new UserProfileDto
        {
            UserId = userId,
            Email = "donor@bistro.com",
            Role = "DONOR",
            BusinessOrOrgName = "Bistro Delight"
        };

        _profileServiceMock
            .Setup(s => s.GetProfileByUserIdAsync(userId))
            .ReturnsAsync(profileDto);

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        var okResult = result as OkObjectResult;

        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetMyProfile_ProfileNotFound_Returns404NotFound()
    {
        // Arrange (TC-PROF-04)
        var userId = Guid.NewGuid();

        SetUserContext(userId);

        _profileServiceMock
            .Setup(s => s.GetProfileByUserIdAsync(userId))
            .ReturnsAsync((UserProfileDto?)null);

        // Act
        var result = await _controller.GetMyProfile();

        // Assert
        var notFoundResult = result as NotFoundObjectResult;

        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateMyProfile_ValidData_Returns200Ok()
    {
        // Arrange (TC-PROF-05)
        var userId = Guid.NewGuid();

        SetUserContext(userId);

        var updateDto = new UpdateProfileDto
        {
            BusinessOrOrgName = "Updated Name"
        };

        var updatedProfile = new UserProfileDto
        {
            UserId = userId,
            BusinessOrOrgName = "Updated Name"
        };

        _profileServiceMock
            .Setup(s => s.UpdateProfileAsync(userId, updateDto))
            .ReturnsAsync((
                true,
                "Profile updated successfully!",
                updatedProfile
            ));

        // Act
        var result = await _controller.UpdateMyProfile(updateDto);

        // Assert
        var okResult = result as OkObjectResult;

        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateMyProfile_ServiceFailure_Returns400BadRequest()
    {
        // Arrange (TC-PROF-07)
        var userId = Guid.NewGuid();

        SetUserContext(userId);

        var updateDto = new UpdateProfileDto
        {
            BusinessOrOrgName = "Updated Name"
        };

        _profileServiceMock
            .Setup(s => s.UpdateProfileAsync(userId, updateDto))
            .ReturnsAsync((
                false,
                "User not found.",
                null
            ));

        // Act
        var result = await _controller.UpdateMyProfile(updateDto);

        // Assert
        var badRequestResult = result as BadRequestObjectResult;

        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetPublicProfile_ExistingUser_Returns200Ok()
    {
        // Arrange (TC-PROF-08)
        var userId = Guid.NewGuid();

        var profileDto = new UserProfileDto
        {
            UserId = userId,
            Role = "DONOR",
            BusinessOrOrgName = "Public Bistro",
            Address = "123 Public Street",
            DonorType = "Restaurant"
        };

        _profileServiceMock
            .Setup(s => s.GetProfileByUserIdAsync(userId))
            .ReturnsAsync(profileDto);

        // Act
        var result = await _controller.GetPublicProfile(userId);

        // Assert
        var okResult = result as OkObjectResult;

        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPublicProfile_NonExistentUser_Returns404NotFound()
    {
        // Arrange (TC-PROF-09)
        var nonExistentId = Guid.NewGuid();

        _profileServiceMock
            .Setup(s => s.GetProfileByUserIdAsync(nonExistentId))
            .ReturnsAsync((UserProfileDto?)null);

        // Act
        var result = await _controller.GetPublicProfile(nonExistentId);

        // Assert
        var notFoundResult = result as NotFoundObjectResult;

        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UploadProfilePicture_ValidFile_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUserContext(userId);

        var fileMock = new Mock<IFormFile>();
        _profileServiceMock
            .Setup(s => s.UploadProfilePictureAsync(userId, fileMock.Object, It.IsAny<string>()))
            .ReturnsAsync((true, "Profile picture updated successfully!", "/uploads/test.png"));

        // Act
        var result = await _controller.UploadProfilePicture(fileMock.Object);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task RemoveProfilePicture_Returns200Ok()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetUserContext(userId);

        _profileServiceMock
            .Setup(s => s.RemoveProfilePictureAsync(userId, It.IsAny<string>()))
            .ReturnsAsync((true, "Profile picture removed successfully."));

        // Act
        var result = await _controller.RemoveProfilePicture();

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPublicProfile_OrganizationUser_ReturnsExpectedFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profileDto = new UserProfileDto
        {
            UserId = userId,
            Role = "ORGANIZATION",
            BusinessOrOrgName = "Charity Org",
            AcceptedFoodCategories = new List<string> { "Cooked Meals" },
            ProfilePictureUrl = "/uploads/test.png"
        };

        _profileServiceMock
            .Setup(s => s.GetProfileByUserIdAsync(userId))
            .ReturnsAsync(profileDto);

        // Act
        var result = await _controller.GetPublicProfile(userId);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        
        // Use reflection to check properties since it returns anonymous object
        var value = okResult.Value;
        value.Should().NotBeNull();
        
        var type = value!.GetType();
        var dataProp = type.GetProperty("data")!.GetValue(value, null);
        dataProp.Should().NotBeNull();
        
        var dataType = dataProp!.GetType();
        dataType.GetProperty("BusinessOrOrgName")!.GetValue(dataProp, null).Should().Be("Charity Org");
        var categories = dataType.GetProperty("AcceptedFoodCategories")!.GetValue(dataProp, null) as List<string>;
        categories.Should().Contain("Cooked Meals");
        dataType.GetProperty("ProfilePictureUrl")!.GetValue(dataProp, null).Should().Be("/uploads/test.png");
    }

    [Fact]
    public async Task GetPublicProfile_DeactivatedAccount_BehavesAccordingToImplementation()
    {
        // Arrange
        var userId = Guid.NewGuid();
        // The implementation does not filter out deactivated accounts at the controller level
        var profileDto = new UserProfileDto
        {
            UserId = userId,
            Role = "DONOR",
            BusinessOrOrgName = "Deactivated User",
            IsActive = false // Deactivated
        };

        _profileServiceMock
            .Setup(s => s.GetProfileByUserIdAsync(userId))
            .ReturnsAsync(profileDto);

        // Act
        var result = await _controller.GetPublicProfile(userId);

        // Assert
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200); // Verify it returns 200 OK
    }
}