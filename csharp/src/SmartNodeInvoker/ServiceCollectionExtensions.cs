using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Train.Solver.SmartNodeInvoker;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSmartNodeInvoker(
        this IServiceCollection services,
        string redisConnectionString, int dbIndex = 3)
    {
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddTransient(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase(dbIndex));
        services.AddSingleton<ISmartNodeInvoker, SmartNodeInvoker>();
        return services;
    }
}
