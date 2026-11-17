using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TmsApi.Api.Filters;

/// <summary>
/// Maps business-rule exceptions thrown by the gradebook service to
/// ProblemDetails responses with the right HTTP status code.
/// Apply with [GradebookExceptionFilter] (the "Attribute" suffix is optional).
/// </summary>
public class GradebookExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var (status, title) = context.Exception switch
        {
            KeyNotFoundException        => (StatusCodes.Status404NotFound, "Not found"),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
            InvalidOperationException   => (StatusCodes.Status409Conflict, "Conflict"),
            ArgumentException           => (StatusCodes.Status400BadRequest, "Invalid request"),
            _                           => (0, "")
        };

        // Unknown exception types fall through to your global error handler.
        if (status == 0) return;

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = context.Exception.Message
        })
        {
            StatusCode = status
        };

        context.ExceptionHandled = true;
    }
}