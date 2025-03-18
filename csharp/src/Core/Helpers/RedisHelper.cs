namespace Train.Solver.Core.Helpers;

public static class RedisHelper
{
    public const string Delimiter = ":";

    public static string BuildLockKey(string network, string address, string? asset = null) 
        => $"{network}{Delimiter}{address}{(asset is not null ? $"{Delimiter}{asset}" : "")}";

    public static string BuildNonceKey(string network, string address, string? asset = null) 
        => $"{network}{Delimiter}{address}{Delimiter}{(asset is not null ? $"{asset}{Delimiter}" : "")}currentNonce";
}
