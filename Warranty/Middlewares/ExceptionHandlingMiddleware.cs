using Microsoft.AspNetCore.Mvc;
using Warranty.Exceptions;

namespace Warranty.Middlewares;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (TryGetStatusCode(exception, out var statusCode))
        {
            logger.LogWarning(exception, "Business request failure returned {StatusCode}. TraceId: {TraceId}",
                statusCode, context.TraceIdentifier);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(CreateProblemDetails(
                statusCode,
                exception.Message,
                context.TraceIdentifier), context.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(CreateProblemDetails(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                context.TraceIdentifier), context.RequestAborted);
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

    private static ProblemDetails CreateProblemDetails(int statusCode, string title, string traceId)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title
        };
        problem.Extensions["traceId"] = traceId;
        return problem;
    }
}
