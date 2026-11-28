using System.Diagnostics;

namespace Warranty.Middlewares;

public sealed class RequestTimingMiddleware(
    RequestDelegate next,
    ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "Request {Method} {Path} processed in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
