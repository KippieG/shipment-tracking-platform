using FluentValidation;
using MediatR;
using Serilog;
using ShipmentTracking.Application.Common.Behaviours;
using ShipmentTracking.Infrastructure.Persistence;
using ShipmentTracking.Infrastructure.Persistence.Seeders;
using ShipmentTracking.WebAPI.Extensions;
using ShipmentTracking.WebAPI.Middleware;

// ── Bootstrap logger (vóór DI) ────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Shipment Tracking Platform opstarten...");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog uit config ────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

    // ── MediatR + behaviours ──────────────────────────────────────────────────
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(
            typeof(ShipmentTracking.Application.Features.Shipments.Commands
                .CreateShipment.CreateShipmentCommand).Assembly));

    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

    // ── FluentValidation ──────────────────────────────────────────────────────
    builder.Services.AddValidatorsFromAssembly(
        typeof(ShipmentTracking.Application.Features.Shipments.Commands
            .CreateShipment.CreateShipmentValidator).Assembly);

    // ── Feature registraties via extensions ───────────────────────────────────
    builder.Services
        .AddDatabase(builder.Configuration)
        .AddRepositories()
        .AddAzureServices()
        .AddAuthServices(builder.Configuration)
        .AddCaching(builder.Configuration)
        .AddRateLimiting()
        .AddHealthChecks()
        .AddSwagger()
        .AddBackgroundServices()
        .AddEmailService();

    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddProblemDetails();
    builder.Services.AddApiVersioning();

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<RequestContextMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shipment Tracking API v1");
            c.RoutePrefix = "swagger";
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
        });
    }

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate = "{RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0}ms)";
    });

    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Health check endpoints
    app.MapHealthChecks("/health/live",  new() { Predicate = c => c.Tags.Contains("live") });
    app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
    app.MapHealthChecks("/health");

    // ── DB migratie + seeding bij startup ─────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (app.Environment.IsDevelopment())
        {
            await db.Database.MigrateAsync();
            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            await seeder.SeedAsync();
        }
    }

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Applicatie onverwacht gestopt.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
