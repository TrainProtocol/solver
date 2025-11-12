using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface ISwapMetricRepository
{
    Task<List<(DateTime Date, decimal Value)>> GetDailyVolumeAsync(DateTime startFrom);
    Task<List<(DateTime Date, decimal Value)>> GetDailyProfitAsync(DateTime startFrom);
    Task<List<(DateTime Date, int Count)>> GetDailyCountAsync(DateTime startFrom);
}
