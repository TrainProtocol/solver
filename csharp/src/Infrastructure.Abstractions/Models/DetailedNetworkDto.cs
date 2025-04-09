﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.API.Models;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Infrastructure.Abstractions.Models;

public class DetailedNetworkDto : NetworkDto
{
    public string Logo { get; set; } = null!;

    public string TransactionExplorerTemplate { get; set; } = null!;

    public string AccountExplorerTemplate { get; set; } = null!;

    public DateTimeOffset ListingDate { get; set; }

    public DetailedTokenDto? NativeToken { get; set; } = null!;

    public IEnumerable<DetailedTokenDto> Tokens { get; set; } = [];

    public IEnumerable<NodeDto> Nodes { get; set; } = [];

    public IEnumerable<ContractDto> Contracts { get; set; } = [];

    public IEnumerable<ManagedAccountDto> ManagedAccounts { get; set; } = [];
}
