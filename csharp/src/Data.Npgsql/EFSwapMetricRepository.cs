using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;

namespace Train.Solver.Data.Npgsql;
internal class EFSwapMetricRepository(SolverDbContext dbContext) : ISwapMetricRepository
{
    public async Task<(decimal TotalVolumeInUsd, decimal TotalProfitInUsd)> GetTotalVolumeAndProfitAsync(DateTime startFrom)
    {
        var result = await dbContext.SwapMetrics
            .Where(m => m.CreatedDate >= startFrom)
            .GroupBy(_ => 1)
            .Select(g => new {
                TotalVolume = g.Sum(x => x.VolumeInUsd),
                TotalProfit = g.Sum(x => x.ProfitInUsd)
            }).FirstOrDefaultAsync();

        return (result?.TotalVolume ?? 0, result?.TotalProfit ?? 0);
    }

    public async Task<List<(DateTime Date, decimal Value)>> GetDailyVolumeAsync(DateTime startFrom)
    {
        var result = await dbContext.SwapMetrics
            .Where(m => m.CreatedDate >= startFrom)
            .GroupBy(m => m.CreatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Value = g.Sum(x => x.VolumeInUsd)
            }).ToListAsync();

        return result.Select(x => (x.Date, x.Value)).ToList();
    }

    public async Task<List<(DateTime Date, decimal Value)>> GetDailyProfitAsync(DateTime startFrom)
    {
        var result = await dbContext.SwapMetrics
            .Where(m => m.CreatedDate >= startFrom)
            .GroupBy(m => m.CreatedDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Value = g.Sum(x => x.ProfitInUsd)
            }).ToListAsync();

        return result.Select(x => (x.Date, x.Value)).ToList();
    }
}
