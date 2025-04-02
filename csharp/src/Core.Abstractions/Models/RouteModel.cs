﻿using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class RouteModel
{
    public int Id { get; set; }

    public TokenModel Source { get; set; } = null!;

    public TokenModel Destionation { get; set; } = null!;

    public decimal MaxAmountInSource { get; set; }

    public RouteStatus Status { get; set; }
}
