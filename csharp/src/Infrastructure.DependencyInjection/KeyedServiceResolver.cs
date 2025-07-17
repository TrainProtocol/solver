using Microsoft.Extensions.DependencyInjection;

namespace Train.Solver.Infrastructure.DependencyInjection;

public class KeyedServiceResolver<T>(IServiceProvider serviceProvider) where T : notnull
{
    public virtual T Resolve(string providerName)
    {
        return serviceProvider.GetRequiredKeyedService<T>(providerName);
    }
}