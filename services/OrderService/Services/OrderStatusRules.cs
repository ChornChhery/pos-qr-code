using OrderService.Domain;

namespace OrderService.Services;

/// <summary>
/// Allowed order status changes. The server enforces them; the client only shows buttons.
/// PENDING -> COOKING -> READY -> COMPLETED, and PENDING or COOKING can be CANCELLED.
/// </summary>
public static class OrderStatusRules
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> Allowed = new()
    {
        [OrderStatus.Pending] = new[] { OrderStatus.Cooking, OrderStatus.Cancelled },
        [OrderStatus.Cooking] = new[] { OrderStatus.Ready, OrderStatus.Cancelled },
        [OrderStatus.Ready] = new[] { OrderStatus.Completed },
        [OrderStatus.Completed] = Array.Empty<OrderStatus>(),
        [OrderStatus.Cancelled] = Array.Empty<OrderStatus>()
    };

    /// <summary>Statuses where the order still needs work and keeps its table occupied.</summary>
    public static readonly OrderStatus[] ActiveStatuses = { OrderStatus.Pending, OrderStatus.Cooking, OrderStatus.Ready };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public static bool IsTerminal(OrderStatus status) =>
        status is OrderStatus.Completed or OrderStatus.Cancelled;
}
