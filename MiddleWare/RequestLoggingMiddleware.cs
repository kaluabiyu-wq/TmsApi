using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a short correlation id and stamp it on the response BEFORE
        // calling next — once the response starts, headers can no longer be set.
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var method = context.Request.Method;
        var path   = context.Request.Path;

        // Entry log
        _logger.LogInformation(
            "Request  {Method} {Path} [{CorrelationId}]",
            method, path, correlationId);

        var stopwatch = Stopwatch.StartNew();

        // Hand control to the rest of the pipeline
        await _next(context);

        stopwatch.Stop();

        // Exit log — status code is only reliable AFTER next() returns
        _logger.LogInformation(
            "Response {Method} {Path} {StatusCode} {ElapsedMs}ms [{CorrelationId}]",
            method, path, context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds, correlationId);
    }
}
