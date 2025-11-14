using Temporalio.Exceptions;
using Temporalio.Workflows;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Blockchain.Solana.Models;
using static Temporalio.Workflows.Workflow;
using Train.Solver.Workflow.Solana.Activities;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Common.Helpers;
using Train.Solver.Workflow.Common.Extensions;
using Train.Solver.Workflow.Solana.Models;

namespace Train.Solver.Workflow.Solana.Workflows;

[Workflow]
public class SolanaTransactionProcessor
{
    [WorkflowRun]
    public async Task<TransactionResponse> RunAsync(TransactionRequest request, TransactionExecutionContext context)
    {
        var preparedTransaction = await ExecuteActivityAsync(
            (ISolanaBlockchainActivities x) => x.BuildTransactionAsync(new TransactionBuilderRequest
            {
                Network = request.Network,
                PrepareArgs = request.PrepareArgs,
                Type = request.Type
            }),
            TemporalHelper.DefaultActivityOptions(request.Network.Type));
                
        var lastValidBLockHash = await ExecuteActivityAsync(
            (ISolanaBlockchainActivities x) => x.GetNextNonceAsync(
                new NextNonceRequest()
                {
                    Address = request.FromAddress,
                    Network = request.Network,
                }),
            TemporalHelper.DefaultActivityOptions(request.Network.Type));

        var rawTx = await ExecuteActivityAsync(
            (ISolanaBlockchainActivities x) => x.ComposeSolanaTranscationAsync(new SolanaComposeTransactionRequest()
            {
                Network = request.Network,
                FromAddress = request.FromAddress,
                CallData = preparedTransaction.Data!
            }),
            TemporalHelper.DefaultActivityOptions(request.Network.Type));

        var signedTx = await ExecuteActivityAsync(
            (ISolanaBlockchainActivities x) => x.SignTransactionAsync(new SolanaSignTransactionRequest()
            {
                Network = request.Network,
                UnsignRawTransaction = rawTx,
                FromAddress = request.FromAddress,
                SignerAgentUrl = request.SignerAgentUrl
            }),
            TemporalHelper.DefaultActivityOptions(request.Network.Type));

        TransactionResponse confirmedTransaction;

        try
        {
            //Simulate transaction
            await ExecuteActivityAsync(
                (ISolanaBlockchainActivities x) => x.SimulateTransactionAsync(
                    new SolanaPublishTransactionRequest()
                    {
                        RawTx = rawTx,
                        Network= request.Network
                    }),
                TemporalHelper.DefaultActivityOptions(request.Network.Type));

            //Send transaction

            var transactionId = await ExecuteActivityAsync(
                (ISolanaBlockchainActivities x) => x.PublishTransactionAsync(
                    new SolanaPublishTransactionRequest()
                    {
                        RawTx = rawTx,
                        Network = request.Network
                    }),
                TemporalHelper.DefaultActivityOptions(request.Network.Type));

            //Wait for transaction receipt

            confirmedTransaction = await ExecuteActivityAsync(
                (ISolanaBlockchainActivities x) => x.GetTransactionAsync(                    
                    request.Network,
                    transactionId),
                TemporalHelper.DefaultActivityOptions(request.Network.Type));
        }
        catch (ActivityFailureException ex)
        {
            if (ex.InnerException is ApplicationFailureException appFailEx &&
               (appFailEx.HasError<NonceMissMatchException>() || appFailEx.HasError<TransactionFailedRetriableException>()))
            {
                throw CreateContinueAsNewException((SolanaTransactionProcessor x) => x.RunAsync(request, context));
            }

            throw;
        }

        return confirmedTransaction;
    }
}
