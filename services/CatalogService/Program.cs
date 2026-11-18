using CatalogService.Data;
using CatalogService.Endpoints;
using CatalogService.Messaging;
using CatalogService.Services;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pos.Shared.Auth;
using Pos.Shared.Http;
using Pos.Shared.Messaging;
using Pos.Shared.Money;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    // Structured JSON logs outside local development.
    builder.Logging.AddJsonConsole();
}

builder.Services.Configure<CurrencyOptions>(builder.Configuration.GetSection(CurrencyOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));

// ---- Database (catalog_db only) ----
var connectionString = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new InvalidOperationException("ConnectionStrings:CatalogDb is not configured.");

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

// ---- Messaging: RabbitMQ with the EF Core transactional outbox and consumer inbox ----
var rabbit = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<CatalogDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<OrderCreatedConsumer>();

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseMessageRetry(r => r.Immediate(5));
        cfg.UseEntityFrameworkOutbox<CatalogDbContext>(context);
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbit.Host, rabbit.Port, rabbit.VirtualHost, h =>
        {
            h.Username(rabbit.Username);
            h.Password(rabbit.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// ---- Application services ----
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CatalogSeeder>();
builder.Services.AddHostedService<CatalogResyncService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---- Authentication, authorization, errors ----
builder.Services.AddPosAuthentication(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "catalog" }));
app.MapCatalogEndpoints();

// ---- Fail fast on bad settings, then migrate and seed ----
_ = app.Services.GetRequiredService<IOptions<PosJwtOptions>>().Value;

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

    if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
    {
        await db.Database.MigrateAsync();
    }

    if (app.Configuration.GetValue("Seed:SampleMenu", false))
    {
        await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync();
    }
}

app.Run();

/// <summary>Exposed so integration tests can use WebApplicationFactory.</summary>
public partial class Program { }
