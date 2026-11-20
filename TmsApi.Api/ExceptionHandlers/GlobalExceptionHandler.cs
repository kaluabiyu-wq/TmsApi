using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using TmsApi.Api.Notifications;

namespace TmsApi.Api.ExceptionHandlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IServiceScopeFactory scopeFactory) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title, detail, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more fields are invalid. See errors for details.",
                (IDictionary<string, string[]>?)ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

          InvalidOperationException ioe => (
                StatusCodes.Status409Conflict,
                "Conflict",
                ioe.Message,
                (IDictionary<string, string[]>?)null),

            UnauthorizedAccessException uae => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                uae.Message,
                (IDictionary<string, string[]>?)null),

            KeyNotFoundException knfe => (
                StatusCodes.Status404NotFound,
                "Not found",
                knfe.Message,
                (IDictionary<string, string[]>?)null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                $"An unexpected error occurred. Trace ID: {httpContext.TraceIdentifier}",
                (IDictionary<string, string[]>?)null),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception (trace={TraceId})", httpContext.TraceIdentifier);
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var notifications = scope.ServiceProvider.GetRequiredService<RoleBasedNotificationService>();
                await notifications.NotifyAdminsAsync(
                    "System error detected",
                    $"An unexpected server error occurred while processing {httpContext.Request.Method} {httpContext.Request.Path}. Reference: {httpContext.TraceIdentifier}.",
                    "System", "Warning", "/admin", ct);
            }
            catch (Exception notificationError)
            {
                // Notification delivery must never mask the original application exception.
                logger.LogWarning(notificationError, "Could not create administrator system-error notification.");
            }
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, ct);

        return true;
    }
}