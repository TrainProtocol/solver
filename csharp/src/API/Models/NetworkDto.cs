﻿namespace Train.Solver.API.Models;

public class NetworkDto
{
    public string Name { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string Logo { get; set; } = null!;

    public string? ChainId { get; set; }

    public string FeeType { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string TransactionExplorerTemplate { get; set; } = null!;

    public string AccountExplorerTemplate { get; set; } = null!;

    public DateTimeOffset ListingDate { get; set; }

    public TokenDto NativeToken { get; set; } = null!;

    public bool IsTestnet{ get; set; } 
}
