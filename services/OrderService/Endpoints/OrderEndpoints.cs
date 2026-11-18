using OrderService.Contracts;
using OrderService.Services;
using OrderService.Validation;
using Pos.Shared.Auth;
using Pos.Shared.Http;

namespace OrderService.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders");

        group.MapGet("/tables", GetTablesAsync);

        group.MapPost("/cart", PriceCartAsync)
            .AddEndpointFilter<ValidationFilter<CartRequest>>();

        group.MapPost("/checkout", CheckoutAsync)
            .AddEndpointFilter<ValidationFilter<CheckoutRequest>>();

        group.MapGet("/", ListOrdersAsync)
            .RequireAuthorization(Policies.AnyStaff);

        // Public on purpose: guests track their order by its random, unguessable id.
        group.MapGet("/{id:guid}", GetOrderAsync);

        group.MapPatch("/{id:guid}/status", UpdateStatusAsync)
            .RequireAuthorization(Policies.OrderStatusUpdaters)
            .AddEndpointFilter<ValidationFilter<UpdateStatusRequest>>();

        return app;
    }

    private static async Task<IResult> GetTablesAsync(OrderWorkflow orders, CancellationToken ct) =>
        Results.Json(ApiResponse<IReadOnlyList<TableDto>>.Ok(await orders.ListTablesAsync(ct)));

    private static async Task<IResult> PriceCartAsync(CartRequest request, OrderWorkflow orders, CancellationToken ct) =>
        (await orders.PriceCartAsync(request, ct)).ToHttp();

    private static async Task<IResult> CheckoutAsync(CheckoutRequest request, OrderWorkflow orders, CancellationToken ct) =>
        (await orders.CheckoutAsync(request, ct)).ToHttp(StatusCodes.Status201Created);

    private static async Task<IResult> ListOrdersAsync(
        OrderWorkflow orders, CancellationToken ct, string? status = null, int page = 1, int pageSize = 50) =>
        (await orders.ListAsync(status, page, pageSize, ct)).ToHttp();

    private static async Task<IResult> GetOrderAsync(Guid id, OrderWorkflow orders, CancellationToken ct) =>
        (await orders.GetAsync(id, ct)).ToHttp();

    private static async Task<IResult> UpdateStatusAsync(
        Guid id, UpdateStatusRequest request, OrderWorkflow orders, CancellationToken ct) =>
        (await orders.UpdateStatusAsync(id, request.Status, ct)).ToHttp();
}
