using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Pos.Shared.Http;

/// <summary>Validates the request body of type T with FluentValidation and returns a 400 envelope on failure.</summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return Results.Json(Api.Fail("Request body is required."), statusCode: StatusCodes.Status400BadRequest);
        }

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<T>>();
        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);

        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).Distinct().ToArray();
            return Results.Json(Api.Fail("Validation failed.", errors), statusCode: StatusCodes.Status400BadRequest);
        }

        return await next(context);
    }
}
