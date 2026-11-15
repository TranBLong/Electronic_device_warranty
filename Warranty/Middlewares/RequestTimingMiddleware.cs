using System.Diagnostics;

namespace Warranty.Middlewares;

public sealed class RequestTimingMiddleware(
    RequestDelegate next,
    ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        await next(context);
        stopwatch.Stop();

        logger.LogInformation(
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {ElapsedMilliseconds} ms. TraceId: {TraceId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds,
            context.TraceIdentifier);
    }
}
