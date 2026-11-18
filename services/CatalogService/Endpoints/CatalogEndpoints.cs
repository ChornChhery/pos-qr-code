using CatalogService.Contracts;
using CatalogService.Data;
using CatalogService.Domain;
using CatalogService.Services;
using CatalogService.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pos.Shared.Auth;
using Pos.Shared.Http;
using Pos.Shared.Money;

namespace CatalogService.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/catalog");

        group.MapGet("/categories", GetCategoriesAsync);
        group.MapGet("/products", GetProductsAsync);
        group.MapGet("/products/{id:guid}", GetProductAsync);

        group.MapPost("/products", CreateProductAsync)
            .RequireAuthorization(Policies.MenuManagers)
            .AddEndpointFilter<ValidationFilter<ProductRequest>>();

        group.MapPut("/products/{id:guid}", UpdateProductAsync)
            .RequireAuthorization(Policies.MenuManagers)
            .AddEndpointFilter<ValidationFilter<ProductRequest>>();

        group.MapPatch("/products/{id:guid}/sold-out", SetSoldOutAsync)
            .RequireAuthorization(Policies.MenuManagers);

        return app;
    }

    private static async Task<IResult> GetCategoriesAsync(CatalogDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description, c.DisplayOrder))
            .ToListAsync(ct);

        return Results.Json(ApiResponse<IReadOnlyList<CategoryDto>>.Ok(categories));
    }

    /// <summary>
    /// Public by default shows products that can be ordered. "includeSoldOut=true" also lists sold-out products,
    /// which the menu can show greyed out and the staff dashboard needs.
    /// </summary>
    private static async Task<IResult> GetProductsAsync(
        CatalogDbContext db, IOptions<CurrencyOptions> currency, HttpContext http, CancellationToken ct,
        Guid? categoryId = null, bool includeSoldOut = false)
    {
        var query = db.Products
            .AsNoTracking()
            .Include(p => p.Modifiers).ThenInclude(m => m.Options)
            .AsSplitQuery()
            .Where(p => p.Category!.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!includeSoldOut)
        {
            query = query.Where(p => p.IsAvailable && (p.StockCount == null || p.StockCount > 0));
        }

        var products = await query.OrderBy(p => p.Name).ToListAsync(ct);
        var includeStock = IsMenuManager(http);
        var dtos = products
            .Select(p => ProductMapper.ToDto(p, currency.Value.UsdToKhrRate, includeStock))
            .ToList();

        return Results.Json(ApiResponse<IReadOnlyList<ProductDto>>.Ok(dtos));
    }

    private static async Task<IResult> GetProductAsync(
        Guid id, CatalogDbContext db, IOptions<CurrencyOptions> currency, HttpContext http, CancellationToken ct)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Modifiers).ThenInclude(m => m.Options)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            return Results.Json(Api.Fail("Product not found."), statusCode: StatusCodes.Status404NotFound);
        }

        var dto = ProductMapper.ToDto(product, currency.Value.UsdToKhrRate, IsMenuManager(http));
        return Results.Json(ApiResponse<ProductDto>.Ok(dto));
    }

    private static async Task<IResult> CreateProductAsync(ProductRequest request, ProductService products, CancellationToken ct) =>
        (await products.CreateAsync(request, ct)).ToHttp(StatusCodes.Status201Created);

    private static async Task<IResult> UpdateProductAsync(
        Guid id, ProductRequest request, ProductService products, CancellationToken ct) =>
        (await products.UpdateAsync(id, request, ct)).ToHttp();

    private static async Task<IResult> SetSoldOutAsync(
        Guid id, SoldOutRequest request, ProductService products, CancellationToken ct) =>
        (await products.SetSoldOutAsync(id, request.IsSoldOut, ct)).ToHttp();

    private static bool IsMenuManager(HttpContext http) =>
        http.User.IsInRole(Roles.Admin) || http.User.IsInRole(Roles.Manager);
}
