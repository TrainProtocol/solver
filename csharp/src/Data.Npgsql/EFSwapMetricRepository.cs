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
    public async Task<(decimal TotalVolumeInUsd, decimal TotalProfitInUsd, int Count)> GetTotalVolumeAndProfitAsync(DateTime startFrom)
    {
        var r = await dbContext.SwapMetrics
            .Where(m => m.CreatedDate >= startFrom)
            .GroupBy(_ => 1)
            .Select(g => new {
                TotalVolume = g.Sum(x => x.VolumeInUsd),
                TotalProfit = g.Sum(x => x.ProfitInUsd),
                Count = g.Count()
            }).FirstOrDefaultAsync();

        return (r?.TotalVolume ?? 0, r?.TotalProfit ?? 0, r?.Count ?? 0);
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
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

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
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return result.Select(x => (x.Date, x.Value)).ToList();
    }

    public async Task<List<(DateTime Date, int Count)>> GetDailyCountAsync(DateTime startFrom)
    {
        var r = await dbContext.SwapMetrics
            .Where(m => m.CreatedDate >= startFrom)
            .GroupBy(m => m.CreatedDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return r.Select(x => (x.Date, x.Count)).ToList();
    }
}
