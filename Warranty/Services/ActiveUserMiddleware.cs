using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Enums;

namespace Warranty.Services;

public sealed class ActiveUserMiddleware(
    RequestDelegate next,
    ILogger<ActiveUserMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                logger.LogWarning("Authenticated request has an invalid user identifier claim. TraceId: {TraceId}",
                    context.TraceIdentifier);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var account = await dbContext.Users.AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => new { user.IsActive, user.Role })
                .SingleOrDefaultAsync(context.RequestAborted);

            var roleClaim = context.User.FindFirst("role")?.Value;
            if (account is null || !account.IsActive || !Enum.TryParse<UserRole>(roleClaim, out var tokenRole) ||
                tokenRole != account.Role)
            {
                logger.LogWarning("Authenticated request rejected because the account or role is no longer valid. TraceId: {TraceId}",
                    context.TraceIdentifier);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next(context);
    }
}
