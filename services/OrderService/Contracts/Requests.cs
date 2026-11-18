namespace OrderService.Contracts;

public sealed record CartItemRequest(Guid ProductId, int Quantity, List<Guid>? ModifierOptionIds, string? Notes);

/// <summary>The cart is held by the client. This request asks the server to price it.</summary>
public sealed record CartRequest(List<CartItemRequest> Items);

public sealed record CheckoutRequest(int TableNumber, string? Notes, List<CartItemRequest> Items);

public sealed record UpdateStatusRequest(string Status);
