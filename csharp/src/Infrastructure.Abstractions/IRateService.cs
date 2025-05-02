using System;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions;

public interface IRateService
{
    Task<decimal> GetRateAsync(Route route);
}
