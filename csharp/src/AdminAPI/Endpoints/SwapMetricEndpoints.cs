using Microsoft.AspNetCore.Mvc;
using Train.Solver.AdminAPI.Models;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.AdminAPI.Endpoints;

public static class SwapMetricEndpoints
{
    public static RouteGroupBuilder MapSwapMetricEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/swap-metrics/daily-volume", GetDailyVolumeAsync)
            .Produces<List<TimeSeriesMetric<decimal>>>();

        group.MapGet("/swap-metrics/daily-profit", GetDailyProfitAsync)
            .Produces<List<TimeSeriesMetric<decimal>>>();

        group.MapGet("/swap-metrics/daily-count", GetDailyCountAsync)
            .Produces<List<TimeSeriesMetric<int>>>();

        return group;
    }

    private static async Task<IResult> GetDailyVolumeAsync(
        ISwapMetricRepository repository,
        [FromQuery] DateTime? startFrom)
    {
        var data = await repository.GetDailyVolumeAsync(startFrom ?? DateTime.UtcNow.AddDays(-30));
        var result = data.Select(x => new TimeSeriesMetric<decimal>
        {
            Date = x.Date,
            Value = x.Value
        }).ToList();
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDailyProfitAsync(
        ISwapMetricRepository repository,
        [FromQuery] DateTime? startFrom)
    {
        var data = await repository.GetDailyProfitAsync(startFrom ?? DateTime.UtcNow.AddDays(-30));
        var result = data.Select(x => new TimeSeriesMetric<decimal>
        {
            Date = x.Date,
            Value = x.Value
        }).ToList();
        return Results.Ok(result);
    }

    private static async Task<IResult> GetDailyCountAsync(
       ISwapMetricRepository repository,
       [FromQuery] DateTime? startFrom)
    {
        var data = await repository.GetDailyCountAsync(startFrom ?? DateTime.UtcNow.AddDays(-30));
        var result = data.Select(x => new TimeSeriesMetric<int>
        {
            Date = x.Date,
            Value = x.Count
        }).ToList();
        return Results.Ok(result);
    }
}