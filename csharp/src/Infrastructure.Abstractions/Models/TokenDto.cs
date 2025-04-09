﻿namespace Train.Solver.API.Models;

public class TokenDto
{
    public string Symbol { get; set; } = null!;

    public string? Contract { get; set; }

    public int Decimals { get; set; }

    public int Precision { get; set; }

    public decimal PriceInUsd { get; set; }  
}