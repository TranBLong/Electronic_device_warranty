using Microsoft.AspNetCore.Mvc;
using Warranty.Exceptions;

namespace Warranty.Middlewares;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (TryGetStatusCode(exception, out var statusCode))
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = statusCode,
                Title = exception.Message
            }, context.RequestAborted);
        }
    }

    private static bool TryGetStatusCode(Exception exception, out int statusCode)
    {
        statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            BusinessRuleException => StatusCodes.Status400BadRequest,
            ConflictException => StatusCodes.Status409Conflict,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            _ => 0
        };
        return statusCode != 0;
    }
}
