using Microsoft.EntityFrameworkCore;
using ShipmentTracking.Infrastructure.Persistence;
using System.Security.Cryptography;
using System.Text;
using ShipmentTracking.WebAPI.Observability;

namespace ShipmentTracking.WebAPI.Middleware;

/// <summary>Replays successful POST responses for a caller-provided idempotency key.</summary>
public sealed class IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
{
    private const string HeaderName = "Idempotency-Key";

    public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
    {
        if (!HttpMethods.IsPost(context.Request.Method) || !context.Request.Path.StartsWithSegments("/api/shipments"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var keyValues) || string.IsNullOrWhiteSpace(keyValues))
        {
            await next(context);
            return;
        }

        var key = keyValues.ToString();
        if (key.Length > 128)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { title = "Idempotency-Key is maximaal 128 tekens." });
            return;
        }

        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(context.RequestAborted);
        context.Request.Body.Position = 0;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
        var scope = $"{context.Request.Method}:{context.Request.Path}";

        var existing = await db.IdempotencyRecords.SingleOrDefaultAsync(x => x.Scope == scope && x.Key == key, context.RequestAborted);
        if (existing is not null)
        {
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(existing.RequestHash), Encoding.UTF8.GetBytes(hash)))
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                Telemetry.IdempotencyConflicts.Add(1);
                await context.Response.WriteAsJsonAsync(new { title = "Idempotency-Key werd al voor een ander request gebruikt." });
                return;
            }

            context.Response.StatusCode = existing.StatusCode;
            Telemetry.IdempotencyReplays.Add(1);
            context.Response.ContentType = "application/json";
            context.Response.Headers.Append("Idempotency-Replayed", "true");
            await context.Response.WriteAsync(existing.ResponseBody, context.RequestAborted);
            return;
        }

        var originalBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;
        try
        {
            await next(context);
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                responseBuffer.Position = 0;
                var response = await new StreamReader(responseBuffer, Encoding.UTF8, leaveOpen: true).ReadToEndAsync(context.RequestAborted);
                db.IdempotencyRecords.Add(IdempotencyRecord.Create(scope, key, hash, context.Response.StatusCode, response));
                await db.SaveChangesAsync(context.RequestAborted);
            }
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Concurrent idempotency key detected for {Scope}", scope);
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            Telemetry.IdempotencyConflicts.Add(1);
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { title = "Hetzelfde Idempotency-Key request wordt al verwerkt." });
        }
        finally
        {
            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalBody, context.RequestAborted);
            context.Response.Body = originalBody;
        }
    }
}
