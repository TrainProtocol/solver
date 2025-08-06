
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Text.Json;
using System.Text.Json.Serialization;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.DependencyInjection;
using Train.Solver.Infrastrucutre.Secret.Treasury.Client;

namespace Train.Solver.Infrastrucutre.Secret.Treasury.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithTreasury(this TrainSolverBuilder builder)
    {
        builder.Services.AddTransient<IPrivateKeyProvider, TreasuryPrivateKeyProvider>();
        return builder;
    }
}
