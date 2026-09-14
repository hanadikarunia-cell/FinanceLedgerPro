using System.Text.Json;
using FinanceLedger.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FinanceLedger.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private const string ProblemJsonContentType = "application/problem+json";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            ValidationAppException => (StatusCodes.Status400BadRequest, "One or more validation errors occurred."),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {Status}: {Message}", status, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://httpstatuses.io/{status}",
            Instance = context.Request.Path
        };

        if (exception is ValidationAppException validation)
        {
            problem.Extensions["errors"] = validation.Errors;
        }

        if (status == StatusCodes.Status500InternalServerError)
        {
            problem.Detail = _environment.IsDevelopment() ? exception.ToString() : "An internal server error occurred.";
        }
        else
        {
            problem.Detail = exception.Message;
        }

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response already started; cannot write problem details.");
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = ProblemJsonContentType;

        var payload = JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(payload);
    }
}
