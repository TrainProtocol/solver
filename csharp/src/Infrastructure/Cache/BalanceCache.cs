using StackExchange.Redis;
using System.Numerics;
using Train.Solver.Common.Extensions;
using Train.Solver.Infrastructure.Abstractions.Cache;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Infrastructure.Cache;

public class BalanceCache(IDatabase cache) : IBalanceCache
{
    static string Key(string address) => $"balance:{address}";

    public async Task SetAsync(string address, NetworkBalanceDto dto, TimeSpan ttl)
    {
        var key = Key(address);
        var json = dto.ToJson();

        var batch = cache.CreateBatch();
        var t1 = batch.HashSetAsync(key, dto.Network.Name, json);
        var t2 = batch.KeyExpireAsync(key, ttl);
        batch.Execute();
        await Task.WhenAll(t1, t2);
    }

    public async Task<NetworkBalanceDto?> GetAsync(string address, string networkName)
    {
        var val = await cache.HashGetAsync(Key(address), networkName);
        return val.HasValue ? val!.ToString().FromJson<NetworkBalanceDto>() : null;
    }

    public async Task<IReadOnlyDictionary<string, NetworkBalanceDto>> GetAllAsync(string address)
    {
        var entries = await cache.HashGetAllAsync(Key(address));
        if (entries.Length == 0) return new Dictionary<string, NetworkBalanceDto>(0);

        var dict = new Dictionary<string, NetworkBalanceDto>(entries.Length);
        foreach (var e in entries)
        {
            var dto = e.Value!.ToString().FromJson<NetworkBalanceDto>();
            if (dto is not null) dict[(string)e.Name] = dto;
        }
        return dict;
    }
}