using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Exceptions;
using OrderManagement.Domain.Exceptions;

namespace OrderManagement.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionHandlingMiddleware> logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Excepción no controlada mientras se procesaba la solicitud.");
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        Exception exception)
    {
        var (statusCode, title, detail) = MapException(exception);
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails);
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
    {
        return exception switch
        {
            ApplicationValidationException or DomainValidationException or InvalidOrderTransitionException
                => (StatusCodes.Status400BadRequest, "Solicitud inválida", exception.Message),
            ProductNotFoundException or OrderNotFoundException
                => (StatusCodes.Status404NotFound, "Recurso no encontrado", exception.Message),
            InsufficientInventoryException
                => (StatusCodes.Status409Conflict, "Inventario insuficiente", exception.Message),
            PaymentProcessingException
                => (StatusCodes.Status503ServiceUnavailable, "Proveedor de pagos no disponible", "El proveedor de pagos no pudo determinar el resultado del pago."),
            _
                => (StatusCodes.Status500InternalServerError, "Error inesperado", "Ocurrió un error inesperado.")
        };
    }
}
