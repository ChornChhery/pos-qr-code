using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrderService.Contracts;
using OrderService.Data;
using OrderService.Domain;
using Pos.Shared.Events;
using Pos.Shared.Http;
using Pos.Shared.Money;

namespace OrderService.Services;

/// <summary>Tables, cart pricing, checkout, and order status changes.</summary>
public sealed class OrderWorkflow(
    OrderDbContext db,
    IPublishEndpoint publish,
    TimeProvider clock,
    IOptions<CurrencyOptions> currency)
{
    private decimal Rate => currency.Value.UsdToKhrRate;

    public async Task<IReadOnlyList<TableDto>> ListTablesAsync(CancellationToken ct = default)
    {
        var tables = await db.Tables
            .AsNoTracking()
            .OrderBy(t => t.TableNumber)
            .Select(t => new { t.Id, t.TableNumber, t.Capacity, t.Status })
            .ToListAsync(ct);

        return tables.Select(t => new TableDto(t.Id, t.TableNumber, t.Capacity, t.Status.ToWire())).ToList();
    }

    public async Task<ServiceResult<CartDto>> PriceCartAsync(CartRequest request, CancellationToken ct = default)
    {
        var priced = await PriceAsync(request.Items, ct);
        if (!priced.Succeeded)
        {
            return ServiceResult<CartDto>.Fail(ErrorKind.Validation, "The cart has problems.", priced.Errors);
        }

        return ServiceResult<CartDto>.Ok(ToCartDto(priced));
    }

    public async Task<ServiceResult<OrderDto>> CheckoutAsync(CheckoutRequest request, CancellationToken ct = default)
    {
        var table = await db.Tables.FirstOrDefaultAsync(t => t.TableNumber == request.TableNumber, ct);
        if (table is null)
        {
            return ServiceResult<OrderDto>.Fail(ErrorKind.NotFound, "Table not found.");
        }

        var priced = await PriceAsync(request.Items, ct);
        if (!priced.Succeeded)
        {
            return ServiceResult<OrderDto>.Fail(ErrorKind.Validation, "The cart has problems.", priced.Errors);
        }

        var now = clock.GetUtcNow();
        var order = new Order
        {
            // Random (not time-ordered) id: guests track their order by this id, so it must not be guessable.
            Id = Guid.NewGuid(),
            TableId = table.Id,
            Status = OrderStatus.Pending,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            Subtotal = priced.Subtotal,
            Tax = 0m,
            Discount = 0m,
            Total = priced.Subtotal,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var line in priced.Lines)
        {
            order.Items.Add(new OrderItem
            {
                Id = Guid.CreateVersion7(),
                OrderId = order.Id,
                ProductId = line.ProductId,
                ProductName = line.ProductName,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                SelectedModifiers = JsonSerializer.Serialize(line.Modifiers),
                Notes = line.Notes,
                TotalPrice = line.LineTotal,
                CreatedAt = now
            });
        }

        await ReserveStockLocallyAsync(priced.Lines, now, ct);

        table.Status = TableStatus.Occupied;
        table.UpdatedAt = now;

        db.Orders.Add(order);
        await publish.Publish(
            new OrderCreated(
                order.Id,
                table.TableNumber,
                order.Total,
                now,
                priced.Lines
                    .Select(l => new OrderLine(
                        l.ProductId,
                        l.ProductName,
                        l.Quantity,
                        l.Notes,
                        l.Modifiers.Select(m => $"{m.GroupName}: {m.Name}").ToList()))
                    .ToList()),
            ct);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<OrderDto>.Fail(
                ErrorKind.Conflict, "Stock just changed. Please check your cart and try again.");
        }

        order.Table = table;
        return ServiceResult<OrderDto>.Ok(ToOrderDto(order));
    }

    public async Task<ServiceResult<OrderDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Table)
            .Include(o => o.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        return order is null
            ? ServiceResult<OrderDto>.Fail(ErrorKind.NotFound, "Order not found.")
            : ServiceResult<OrderDto>.Ok(ToOrderDto(order));
    }

    public async Task<ServiceResult<PagedResult<OrderDto>>> ListAsync(
        string? status, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!EnumWire.TryParseWire<OrderStatus>(status, out var parsed))
            {
                return ServiceResult<PagedResult<OrderDto>>.Fail(
                    ErrorKind.Validation, "Status must be one of PENDING, COOKING, READY, COMPLETED, CANCELLED.");
            }

            query = query.Where(o => o.Status == parsed);
        }

        var total = await query.CountAsync(ct);
        var orders = await query
            .Include(o => o.Table)
            .Include(o => o.Items)
            .AsSplitQuery()
            .OrderBy(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = orders.Select(ToOrderDto).ToList();
        return ServiceResult<PagedResult<OrderDto>>.Ok(new PagedResult<OrderDto>(dtos, page, pageSize, total));
    }

    public async Task<ServiceResult<OrderDto>> UpdateStatusAsync(Guid id, string statusText, CancellationToken ct = default)
    {
        if (!EnumWire.TryParseWire<OrderStatus>(statusText, out var target))
        {
            return ServiceResult<OrderDto>.Fail(
                ErrorKind.Validation, "Status must be one of PENDING, COOKING, READY, COMPLETED, CANCELLED.");
        }

        var order = await db.Orders
            .Include(o => o.Table)
            .Include(o => o.Items)
            .AsSplitQuery()
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
        {
            return ServiceResult<OrderDto>.Fail(ErrorKind.NotFound, "Order not found.");
        }

        var previous = order.Status;
        if (!OrderStatusRules.CanTransition(previous, target))
        {
            return ServiceResult<OrderDto>.Fail(
                ErrorKind.Conflict, $"An order cannot go from {previous.ToWire()} to {target.ToWire()}.");
        }

        var now = clock.GetUtcNow();
        order.Status = target;
        order.UpdatedAt = now;

        if (OrderStatusRules.IsTerminal(target))
        {
            order.CompletedAt = now;

            // Free the table when this was its last active order.
            var otherActive = await db.Orders.AnyAsync(
                o => o.TableId == order.TableId && o.Id != order.Id && OrderStatusRules.ActiveStatuses.Contains(o.Status), ct);

            if (!otherActive && order.Table is not null)
            {
                order.Table.Status = TableStatus.Available;
                order.Table.UpdatedAt = now;
            }
        }

        await publish.Publish(
            new OrderStatusChanged(order.Id, order.Table?.TableNumber ?? 0, previous.ToWire(), target.ToWire(), now), ct);

        await db.SaveChangesAsync(ct);
        return ServiceResult<OrderDto>.Ok(ToOrderDto(order));
    }

    private async Task<PricingResult> PriceAsync(IReadOnlyList<CartItemRequest> items, CancellationToken ct)
    {
        var inputs = items
            .Select(i => new CartItemInput(i.ProductId, i.Quantity, i.ModifierOptionIds, i.Notes))
            .ToList();

        var ids = inputs.Select(i => i.ProductId).Distinct().ToList();
        var stored = await db.CatalogProducts.AsNoTracking().Where(p => ids.Contains(p.Id)).ToListAsync(ct);
        var products = stored.ToDictionary(p => p.Id, ToPricingProduct);

        return CartPricing.Price(inputs, products);
    }

    /// <summary>
    /// Lowers the local stock copy right away so the next guest sees the new count before Catalog confirms it.
    /// Catalog's own count arrives later as StockChanged and replaces this value.
    /// </summary>
    private async Task ReserveStockLocallyAsync(IReadOnlyList<PricedLine> lines, DateTimeOffset now, CancellationToken ct)
    {
        var quantities = lines.GroupBy(l => l.ProductId).ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        var ids = quantities.Keys.ToList();
        var tracked = await db.CatalogProducts.Where(p => ids.Contains(p.Id) && p.StockCount != null).ToListAsync(ct);

        foreach (var product in tracked)
        {
            product.StockCount = Math.Max(product.StockCount!.Value - quantities[product.Id], 0);
            product.UpdatedAt = now;
        }
    }

    private CartDto ToCartDto(PricingResult priced) =>
        new(
            priced.Lines
                .Select(l => new CartLineDto(
                    l.ProductId,
                    l.ProductName,
                    l.Quantity,
                    l.UnitPrice,
                    CurrencyConverter.ToKhr(l.UnitPrice, Rate),
                    l.Modifiers.Select(m => new SelectedModifierDto(m.OptionId, m.GroupName, m.Name, m.PriceAdd)).ToList(),
                    l.Notes,
                    l.LineTotal,
                    CurrencyConverter.ToKhr(l.LineTotal, Rate)))
                .ToList(),
            priced.Subtotal,
            0m,
            0m,
            priced.Subtotal,
            CurrencyConverter.ToKhr(priced.Subtotal, Rate),
            Rate);

    private OrderDto ToOrderDto(Order order) =>
        new(
            order.Id,
            order.Table?.TableNumber ?? 0,
            order.Status.ToWire(),
            order.Notes,
            order.Subtotal,
            order.Tax,
            order.Discount,
            order.Total,
            CurrencyConverter.ToKhr(order.Total, Rate),
            order.CreatedAt,
            order.UpdatedAt,
            order.CompletedAt,
            order.Items
                .OrderBy(i => i.CreatedAt)
                .Select(i => new OrderItemDto(
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice,
                    CurrencyConverter.ToKhr(i.UnitPrice, Rate),
                    ParseModifiers(i.SelectedModifiers),
                    i.Notes,
                    i.TotalPrice,
                    CurrencyConverter.ToKhr(i.TotalPrice, Rate)))
                .ToList());

    private static List<SelectedModifierDto> ParseModifiers(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<SelectedModifierDto>();
        }

        var modifiers = JsonSerializer.Deserialize<List<PricedModifier>>(json) ?? new List<PricedModifier>();
        return modifiers.Select(m => new SelectedModifierDto(m.OptionId, m.GroupName, m.Name, m.PriceAdd)).ToList();
    }

    private static PricingProduct ToPricingProduct(CatalogProduct product)
    {
        var modifiers = JsonSerializer.Deserialize<List<ModifierSnapshot>>(product.ModifiersJson) ?? new List<ModifierSnapshot>();
        return new PricingProduct(product.Id, product.Name, product.Price, product.IsAvailable, product.StockCount, modifiers);
    }
}
