using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderService.Data;
using OrderService.Endpoints;
using OrderService.Messaging;
using OrderService.Services;
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

// ---- Database (order_db only) ----
var connectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?? throw new InvalidOperationException("ConnectionStrings:OrderDb is not configured.");

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

// ---- Messaging: RabbitMQ with the EF Core transactional outbox and consumer inbox ----
var rabbit = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<ProductSnapshotConsumer>();
    x.AddConsumer<StockChangedConsumer>();

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseMessageRetry(r => r.Immediate(5));
        cfg.UseEntityFrameworkOutbox<OrderDbContext>(context);
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
builder.Services.AddScoped<OrderWorkflow>();
builder.Services.AddScoped<TableSeeder>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ---- Authentication, authorization, errors ----
builder.Services.AddPosAuthentication(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "order" }));
app.MapOrderEndpoints();

// ---- Fail fast on bad settings, then migrate and seed ----
_ = app.Services.GetRequiredService<IOptions<PosJwtOptions>>().Value;

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

    if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
    {
        await db.Database.MigrateAsync();
    }

    await scope.ServiceProvider.GetRequiredService<TableSeeder>().SeedAsync();
}

app.Run();

/// <summary>Exposed so integration tests can use WebApplicationFactory.</summary>
public partial class Program { }
