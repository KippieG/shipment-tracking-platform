using FluentValidation;
using ShipmentTracking.Domain.Exceptions;
using System.Net;
using System.Text.Json;

namespace ShipmentTracking.WebAPI.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Onbehandelde uitzondering voor {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, errors) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "Validatiefout",
                ve.Errors.Select(e => e.ErrorMessage).ToList()),

            ShipmentNotFoundException or
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                exception.Message,
                new List<string>()),

            DomainException => (
                HttpStatusCode.UnprocessableEntity,
                exception.Message,
                new List<string>()),

            UnauthorizedException => (
                HttpStatusCode.Unauthorized,
                exception.Message,
                new List<string>()),

            ForbiddenException => (
                HttpStatusCode.Forbidden,
                exception.Message,
                new List<string>()),

            ConflictException => (
                HttpStatusCode.Conflict,
                exception.Message,
                new List<string>()),

            OperationCanceledException => (
                HttpStatusCode.ServiceUnavailable,
                "Aanvraag geannuleerd.",
                new List<string>()),

            _ => (
                HttpStatusCode.InternalServerError,
                "Er is een onverwachte fout opgetreden.",
                new List<string>())
        };

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var response = new
        {
            type = $"https://httpstatuses.io/{(int)statusCode}",
            title,
            status = (int)statusCode,
            errors,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}

/// <summary>
/// Voegt een correlatie-ID toe aan elk request voor tracing.
/// </summary>
public sealed class RequestContextMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Response.Headers[CorrelationIdHeader] = correlationId;
        context.Items[CorrelationIdHeader] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}

/// <summary>
/// CurrentUserService — leest claims uit JWT-token.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ShipmentTracking.Application.Common.Interfaces.ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string UserId =>
        User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";

    public string UserName =>
        User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "anonymous";

    public string UserEmail =>
        User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
