using Microsoft.AspNetCore.Mvc;

namespace PricingAndTrading.Api.Controllers;

internal static class ApiValidationProblem
{
    public static BadRequestObjectResult Create(
        IReadOnlyDictionary<string, string[]> errors) =>
        new(new ValidationProblemDetails(
            errors.ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.Ordinal))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        });

    public static BadRequestObjectResult Create(
        string field,
        string message) =>
        Create(new Dictionary<string, string[]>
        {
            [field] = [message]
        });
}
