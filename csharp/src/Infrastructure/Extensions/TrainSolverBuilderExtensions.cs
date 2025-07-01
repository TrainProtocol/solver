using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Temporalio.Client;
using Temporalio.Exceptions;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastructure.Services;

namespace Train.Solver.Infrastructure.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithCoreServices(
        this TrainSolverBuilder builder)
    {
        builder.Services.AddTransient<IWalletService, WalletService>();
        builder.Services.AddTransient<INetworkService, NetworkService>();
        return builder;
    }
}