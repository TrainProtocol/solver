using System.Net;
using System.Text.Json;
using FluentResults;
using Serilog;
using Train.Solver.API.Extensions;
using Train.Solver.API.Models;
using Train.Solver.Core.Errors;

namespace Train.Solver.API.MIddlewares;

public class ErrorHandlerMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (BadHttpRequestException)
        {
            await HandleErrorAsync(httpContext, new InvalidRequestBodyError());
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception happened during processing request.");
            await HandleErrorAsync(httpContext, new UnhandledError());
        }
    }

    private static async Task HandleErrorAsync(HttpContext context, IError error)
    {
        context.Response.Clear();

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = error is BaseError baseError ? baseError.HttpStatusCode : (int)HttpStatusCode.BadRequest;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(
                new ApiResponse
                {
                    Error = error.ToApiError()
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }));
    }
}
