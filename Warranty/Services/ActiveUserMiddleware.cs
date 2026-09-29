using Microsoft.EntityFrameworkCore;
using Warranty.Data;
using Warranty.Enums;

namespace Warranty.Services;

public sealed class ActiveUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
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
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next(context);
    }
}
