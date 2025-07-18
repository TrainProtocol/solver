using System.Net;
using System.Text.Json;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.PublicAPI.Models;

namespace Train.Solver.PublicAPI.MIddlewares;

public class ErrorHandlerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (BadHttpRequestException e)
        {
            await HandleErrorAsync(httpContext, e);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(httpContext, ex);
        }
    }

    private static async Task HandleErrorAsync(HttpContext context,Exception e)
    {
        context.Response.Clear();

        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An unexpected error occurred. Please try again later.";



        if (e is UserFacingException)
        {
            statusCode = HttpStatusCode.BadRequest;
            message = e.Message;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;


        await context.Response.WriteAsync(
            JsonSerializer.Serialize(
                new ApiResponse
                {
                    Error = new ApiError()
                    {
                        //Code = statusCode.ToString(),
                        Message = message
                    }
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }));
    }
}
