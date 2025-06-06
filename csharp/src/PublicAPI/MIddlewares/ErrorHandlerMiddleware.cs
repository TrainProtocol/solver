﻿using System.Net;
using System.Text.Json;
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

        context.Response.ContentType = "application/json";
        
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(
                new ApiResponse
                {
                    Error = new ApiError()
                    {
                        Code = HttpStatusCode.InternalServerError.ToString(),
                        Message = "An unexpected error occurred. Please try again later."
                    }
                },
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                }));
    }
}
