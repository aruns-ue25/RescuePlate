using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryWorkflowService.UnitTests.Helpers;

public static class ControllerTestHelper
{
    public static ControllerContext CreateControllerContext(string userId, string role = "DONOR")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim("id", userId),
            new Claim("role", role)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}
