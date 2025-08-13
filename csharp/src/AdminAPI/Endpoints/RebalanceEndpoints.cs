using Microsoft.AspNetCore.Mvc;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Converters;
using Train.Solver.AdminAPI.Models;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Extensions;
using Train.Solver.Common.Helpers;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Common.Helpers;

namespace Train.Solver.AdminAPI.Endpoints;

public static class RebalanceEndpoints
{
    public static RouteGroupBuilder MapRebalanceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/rebalance", GetAllAsync)
          .Produces<List<RebalanceEntry>>(StatusCodes.Status200OK);

        group.MapPost("/rebalance", RebalanceAsync)
            .Produces(StatusCodes.Status200OK);

        return group;
    }

    private static async Task<IResult> GetAllAsync(
        ITemporalClient temporalClient)
    {
        var query = $"`WorkflowId` STARTS_WITH \"Rebalance\"";
        var results = new List<RebalanceEntry>();

        await foreach (var wf in temporalClient.ListWorkflowsAsync(query, new WorkflowListOptions
        {
            Limit = 10,
        }))
        {
            var whHandle = temporalClient.GetWorkflowHandle(wf.Id);
            var describedWorkflow = await whHandle.DescribeAsync();

            if (describedWorkflow.Memo.TryGetValue("Summary", out var summary) && summary != null)
            {
                var decoded = temporalClient.Options.DataConverter.ToValueAsync<string>(summary.Payload);

                try
                {
                    var rebalanceEntry = new RebalanceEntry
                    {
                        Id = wf.Id,
                        Status = wf.Status.ToString(),
                        Summary = decoded.Result.FromJson<RebalanceSummary>(),
                    };

                    if (wf.Status == WorkflowExecutionStatus.Completed)
                    {
                        var wfResult = await whHandle.GetResultAsync<TransactionResponse>();

                        rebalanceEntry.Transaction = new TransactionDto
                        {
                            Hash = wfResult.TransactionHash,
                            Network = wfResult.NetworkName,
                            Type = TransactionType.Transfer,
                        };
                    }

                    results.Add(rebalanceEntry);
                }
                catch (Exception)
                {
                }
            }
        }

        return Results.Ok(results);
    }

    private static async Task<IResult> RebalanceAsync(
        INetworkRepository repository,
        IWalletRepository walletRepository,
        ITrustedWalletRepository trustedWalletRepository,
        ITemporalClient temporalClient,
        [FromBody] RebalanceRequest request)
    {
        var network = await repository.GetAsync(request.NetworkName);

        if (network == null)
        {
            return Results.NotFound($"Network {request.NetworkName} not found");
        }

        var token = network.Tokens
            .FirstOrDefault(x => x.Asset == request.Token);

        if (token is null)
        {
            return Results.NotFound($"Token {request.Token} not found on network {request.NetworkName}");
        }

        var wallet = await walletRepository.GetAsync(network.Type, request.FromAddress);

        if (wallet is null)
        {
            return Results.NotFound($"Wallet {request.FromAddress} not found on network {request.NetworkName}");
        }

        string toAddress;
        var trustedWallet = await trustedWalletRepository.GetAsync(network.Type, request.ToAddress);

        if (trustedWallet is null)
        {
            var toWallet = await walletRepository.GetAsync(network.Type, request.ToAddress);

            if (toWallet == null)
            {
                return Results.BadRequest($"To address {request.ToAddress} is not a trusted wallet, but a regular wallet.");
            }

            toAddress = toWallet.Address;
        }
        else
        {
            toAddress = trustedWallet.Address;
        }

        var summary = new RebalanceSummary
        {
            Amount = request.Amount.ToString(),
            Network = network.ToExtendedDto(),
            Token = token.ToDto(),
            From = wallet.Address,
            To = toAddress,
        };

        var workflowId = await temporalClient.StartWorkflowAsync(
                    TemporalHelper.ResolveProcessor(network.Type), [new TransactionRequest()
                        {
                            PrepareArgs = new TransferPrepareRequest
                            {
                              Amount = request.Amount,
                              Asset = token.Asset,
                              FromAddress = wallet.Address,
                              ToAddress = toAddress,
                            }.ToJson(),
                            Type = TransactionType.Transfer,
                            Network = network.ToDetailedDto(),
                            FromAddress = wallet.Address,
                            SignerAgentUrl = wallet.SignerAgent.Url,
                    },
                    new TransactionExecutionContext()],
                    new(id: TemporalHelper.BuildRebalanceProcessorId(network.Name, Guid.NewGuid()),
                    taskQueue: network.Type.ToString())
                    {
                        IdReusePolicy = WorkflowIdReusePolicy.TerminateIfRunning,
                        Memo = new Dictionary<string, object>
                        {
                            { "Summary", summary.ToJson()},
                        }
                    });

        return Results.Ok(workflowId);
    }
}
