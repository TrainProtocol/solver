﻿using Train.Solver.Data.Abstractions.Entities.Base;

namespace Train.Solver.Data.Abstractions.Entities;

public enum RouteStatus
{
    Active,
    Inactive,
    Archived,
}

public class Route : EntityBase<int>
{
    public decimal MaxAmountInSource { get; set; }

    public int SourceTokenId { get; set; }

    public int DestinationTokenId { get; set; }

    public RouteStatus Status { get; set; }

    public Token SourceToken { get; set; } = null!;

    public Token DestinationToken { get; set; } = null!;
}