﻿using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class NetworkDto
{
    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string? ChainId { get; set; }

    public TransactionFeeType FeeType { get; set; }

    public NetworkType Type { get; set; }

    public bool IsTestnet { get; set; }
}
