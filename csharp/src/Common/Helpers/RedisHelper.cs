namespace Train.Solver.Common.Helpers;

public static class RedisHelper
{
    public const string Delimiter = ":";

    public static string BuildLockKey(string network, string address) 
        => $"{network}{Delimiter}{address}";

    public static string BuildNonceKey(string network, string address) 
        => $"{network}{Delimiter}{address}{Delimiter}currentNonce";

    public static string BuildNodeScoreKey(string network)
        => $"{network}{Delimiter}nodeScoreboard";
}
