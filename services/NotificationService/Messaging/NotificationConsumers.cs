using MassTransit;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Hubs;
using Pos.Shared.Events;

namespace NotificationService.Messaging;

/// <summary>Pushes order and stock events to the staff dashboards over SignalR.</summary>
public sealed class StaffNotificationConsumer(IHubContext<KitchenHub> kitchen, IHubContext<CashierHub> cashier)
    : IConsumer<OrderCreated>, IConsumer<OrderStatusChanged>, IConsumer<LowStockAlert>
{
    public Task Consume(ConsumeContext<OrderCreated> context) =>
        PushAsync(HubEvents.OrderCreated, context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<OrderStatusChanged> context) =>
        PushAsync(HubEvents.OrderStatusChanged, context.Message, context.CancellationToken);

    public Task Consume(ConsumeContext<LowStockAlert> context) =>
        PushAsync(HubEvents.LowStockAlert, context.Message, context.CancellationToken);

    private Task PushAsync<T>(string method, T message, CancellationToken ct) =>
        Task.WhenAll(
            kitchen.Clients.All.SendAsync(method, message, ct),
            cashier.Clients.All.SendAsync(method, message, ct));
}
