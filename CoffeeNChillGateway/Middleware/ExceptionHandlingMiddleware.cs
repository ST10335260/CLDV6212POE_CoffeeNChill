using System.Text.Json;
using Azure;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Middleware;

/// <summary>
/// Global exception handler. Turns any unhandled exception into a ProblemDetails JSON response.
/// Written as custom middleware (Microsoft, n.d.i). The ProblemDetails error format follows Microsoft (n.d.d).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            int status;
            string title;
            switch (ex)
            {
                case HttpRequestException:
                    status = StatusCodes.Status502BadGateway;
                    title = "The Azure Functions backend could not be reached.";
                    break;
                case RequestFailedException:
                    status = StatusCodes.Status502BadGateway;
                    title = "An Azure Storage request failed.";
                    break;
                case BadHttpRequestException:
                    status = StatusCodes.Status400BadRequest;
                    title = "The request was not valid.";
                    break;
                default:
                    status = StatusCodes.Status500InternalServerError;
                    title = "An unexpected error occurred.";
                    break;
            }

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Instance = context.Request.Path
            };

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
