using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Pos.Shared.Auth;

namespace NotificationService.Hubs;

/// <summary>
/// Kitchen live updates (KITCHEN, MANAGER, ADMIN). Server to client events:
/// OrderCreated, OrderStatusChanged, LowStockAlert. Clients cannot send events.
/// </summary>
[Authorize(Policy = Policies.KitchenHub)]
public sealed class KitchenHub : Hub
{
}

/// <summary>
/// Cashier live updates (CASHIER, MANAGER, ADMIN). Server to client events:
/// OrderCreated, OrderStatusChanged, LowStockAlert. Clients cannot send events.
/// </summary>
[Authorize(Policy = Policies.CashierHub)]
public sealed class CashierHub : Hub
{
}

public static class HubEvents
{
    public const string OrderCreated = "OrderCreated";
    public const string OrderStatusChanged = "OrderStatusChanged";
    public const string LowStockAlert = "LowStockAlert";
}
