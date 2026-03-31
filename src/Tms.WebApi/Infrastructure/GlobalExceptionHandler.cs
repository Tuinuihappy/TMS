using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tms.SharedKernel.Exceptions;

namespace Tms.WebApi.Infrastructure;

/// <summary>RFC 7807 Problem Details global exception handler</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            DomainException de => (422, "Business Rule Violation", de.Message, (Dictionary<string, string[]>?)null),
            NotFoundException nf => (404, "Not Found", nf.Message, null),
            ValidationException ve => (400, "Validation Error", "One or more validation errors occurred.",
                ve.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
            ArgumentException ae => (400, "Bad Request", ae.Message, null),
            _ => (500, "Internal Server Error", "An unexpected error occurred.", null)
        };

        if (statusCode >= 500)
            logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            logger.LogWarning("Handled exception ({StatusCode}): {Message}", statusCode, exception.Message);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = httpContext.Request.Path
        };

        if (errors is not null)
            problemDetails.Extensions["errors"] = errors;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: ct);
        return true;
    }
}
