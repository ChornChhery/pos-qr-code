using MassTransit;
using Microsoft.Extensions.Options;
using NotificationService.Hubs;
using NotificationService.Messaging;
using Pos.Shared.Auth;
using Pos.Shared.Http;
using Pos.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    // Structured JSON logs outside local development.
    builder.Logging.AddJsonConsole();
}

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
var rabbit = builder.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

// This service has no database. Events are pushed to connected dashboards and not stored.
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StaffNotificationConsumer>();

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

builder.Services.AddSignalR();

// Browsers cannot set headers on WebSockets, so hub requests may carry the token as ?access_token=.
builder.Services.AddPosAuthentication(builder.Configuration, allowQueryToken: true);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "notification" }));
app.MapHub<KitchenHub>("/hubs/kitchen");
app.MapHub<CashierHub>("/hubs/cashier");

// Fail fast on bad JWT settings.
_ = app.Services.GetRequiredService<IOptions<PosJwtOptions>>().Value;

app.Run();

/// <summary>Exposed so integration tests can use WebApplicationFactory.</summary>
public partial class Program { }
