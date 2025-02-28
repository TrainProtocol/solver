var a = 1;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;
//using Temporalio.Client;
//using Train.Solver.Core.Blockchain.Models;
//using Train.Solver.Core.DependencyInjection;
//using Train.Solver.Core.Extensions;
//using Train.Solver.Data;
//using Train.Solver.Data.Entities;
//using Train.Solver.WorkflowRunner.Workflows;

//var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
//        .AddLayerswapAzureAppConfiguration();

//var services = new ServiceCollection();

//services.AddTrainSolver(configuration);
//var serviceProvider = services.BuildServiceProvider();

//var temporalClient = serviceProvider.GetService<ITemporalClient>();
//var temporalOptions = serviceProvider.GetService<IOptions<TemporalOptions>>();


////List<string> networks = ["ETHEREUM_SEPOLIA", "ARBITRUM_SEPOLIA", "OPTIMISM_SEPOLIA"];

////foreach (var network in networks)
////{
////    await temporalClient.StartWorkflowAsync(
////    (EventListenerWorkflow x) =>
////        x.RunAsync(network, 5, TimeSpan.FromSeconds(20), null),
////               new(id: EventListenerWorkflow.BuildWorkflowId(network), taskQueue: temporalOptions.Value.TaskQueue)
////               {
////                   IdReusePolicy = Temporalio.Api.Enums.V1.WorkflowIdReusePolicy.TerminateIfRunning,
////               });
////}


//var db = serviceProvider.GetService<SolverDbContext>();

//var swaps = await db.Swaps
//    .Include(x => x.Transactions)
//    .Include(x => x.DestinationToken.Network)
//    .ThenInclude(x => x.ManagedAccounts)
//    .Where(x =>
//        !x.Transactions.Any(t => t.Type == TransactionType.HTLCRedeem
//                                 || t.Type == TransactionType.HTLCRefund)
//        && x.Transactions.Any(t => t.Type == TransactionType.HTLCLock)
//    )
//    .ToListAsync();

//foreach (var swap in swaps.Where(x => x.SourceToken.Network.Group.ToString().StartsWith("EVM")))
//{
//    await temporalClient.StartWorkflowAsync(
//        (TransactionWorkflow x) => x.ExecuteTransactionAsync(
//            new()
//            {
//                PrepareArgs = new HTLCRefundTransactionPrepareRequest
//                {
//                    Id = swap.Id,
//                    Asset = swap.DestinationToken.Asset,
//                }.ToArgs(),
//                Type = TransactionType.HTLCRefund,
//                CorrelationId = swap.Id,
//                NetworkName = swap.DestinationToken.Network.Name,
//                FromAddress = swap.DestinationToken.Network.ManagedAccounts.First().Address,
//            }, swap.Id),
//                new(id: TransactionWorkflow.BuildId(swap.DestinationToken.Network.Name, TransactionType.HTLCRefund), taskQueue: temporalOptions.Value.TaskQueue)
//                {
//                    IdReusePolicy = Temporalio.Api.Enums.V1.WorkflowIdReusePolicy.TerminateIfRunning,
//                });
//}

