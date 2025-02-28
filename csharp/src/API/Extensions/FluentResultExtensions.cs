using FluentResults;
using Serilog;
using Train.Solver.API.Models;
using Train.Solver.Core.Errors;

namespace Train.Solver.API.Extensions;

public static class FluentResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsFailed)
        {
            return result.ToResult().ToHttpResult();
        }

        return Results.Ok(new ApiResponse<T>
        {
            Data = result.Value
        });
    }

    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsFailed)
        {
            var error = result.Errors.First();
            Log.Debug($"Http result error: {error.Message}");

            int statusCode = StatusCodes.Status400BadRequest;

            if (error is BaseError baseError)
            {
                statusCode = baseError.HttpStatusCode;
            }

            return Results.Json(
                data: new ApiResponse
                {
                    Error = error.ToApiError()
                },
                statusCode: statusCode);
        }

        return Results.Ok(new ApiResponse());
    }

    public static ApiError ToApiError(this IError error)
    {
        var errorCode = "UNEXPECTED_ERROR";

        if (error is BaseError baseError)
        {
            errorCode = baseError.ErrorCode;
        }

        return new()
        {
            Code = errorCode,
            Message = error.Message,
            Metadata = error.Metadata,
        };
    }
}
