﻿using Temporalio.Extensions.Hosting;
using Train.Solver.Blockchains.Solana.Activities;
using Train.Solver.Blockchains.Solana.Workflows;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.DependencyInjection;
using Train.Solver.Core.Workflows.Worklows;

namespace Train.Solver.Blockchains.Solana.Extensions;

public static class TrainSolverBuilderExtensions
{
    public static TrainSolverBuilder WithSolanaWorkflows(
     this TrainSolverBuilder builder)
    {
        var temporalBuilder = builder.Services
            .AddHostedTemporalWorker(nameof(NetworkType.Solana))
            .AddWorkflow<SolanaTransactionProcessor>()
            .AddWorkflow<EventListenerWorkflow>()
            .AddTransientActivities<SolanaBlockchainActivities>();

        return builder;
    }
}