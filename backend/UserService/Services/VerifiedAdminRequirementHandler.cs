using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UserService.Data;

namespace UserService.Services;

public class VerifiedAdminRequirement : IAuthorizationRequirement
{
}

public class VerifiedAdminRequirementHandler : AuthorizationHandler<VerifiedAdminRequirement>
{
    private readonly IServiceProvider _serviceProvider;

    public VerifiedAdminRequirementHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VerifiedAdminRequirement requirement)
    {
        if (context.User == null || !context.User.Identity?.IsAuthenticated == true)
        {
            return;
        }

        // 1. Role Claim Check
        var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (roleClaim != "ADMIN")
        {
            return;
        }

        // 2. Verified Admin Claim Check
        var isVerifiedClaim = context.User.FindFirst("admin_verified")?.Value;
        if (isVerifiedClaim != "true")
        {
            return;
        }

        // 3. Real-Time Account Active Status Check (Immediate Token Revocation)
        var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RescuePlateDbContext>();
            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null && !user.IsActive)
            {
                // Account explicitly deactivated in DB -> Revoke access immediately
                return;
            }

            var isActiveClaim = context.User.FindFirst("IsActive")?.Value;
            if (user == null && isActiveClaim == "False")
            {
                return;
            }
        }

        context.Succeed(requirement);
    }
}
