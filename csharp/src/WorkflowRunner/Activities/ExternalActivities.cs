using Microsoft.EntityFrameworkCore;
using Temporalio.Activities;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

namespace Train.Solver.WorkflowRunner.Activities;

public class ExternalActivities(
    SolverDbContext dbContext,
    IKeyedServiceProvider serviceProvider)
{
    [Activity]
    public async Task<JsSufficientBalanceRequest> ExternalSufficientBalanceRequestAsync(
        string networkName,
        string address,
        string asset,
        decimal amount,
        string? correlationId)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network == null)
        {
            throw new($"Network is not configured on {networkName} network");
        }

        var currency = network.Tokens.FirstOrDefault(
            x => x.Asset.ToUpper() == asset.ToUpper());

        if (currency is null)
        {
            throw new($"Failed to get balance since Asset:{asset} is missing for network: {networkName}");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new($"Failed to get balance since {NodeType.Primary} node is missing for network: {networkName}");
        }

        var request = new JsSufficientBalanceRequest
        {
            CorrelationId = correlationId,
            NodeUrl = node.Url,
            Decimals = currency.Decimals,
            TokenContract = currency.TokenContract,
            Symbol = currency.Asset.ToUpper(),
            Amount = amount,
            Address = address
        };

        return request;
    }

    [Activity]
    public async Task<JsGetFeesRequest> ExternalFeeRequestAsync(
        string networkName,
        EstimateFeeRequest request,
        string? correlationId)
    {
        var network = await dbContext.Networks
            .Include(x => x.Tokens)
            .Include(x => x.Nodes)
            .Include(x => x.ManagedAccounts)
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network == null)
        {
            throw new($"Chain is not configured on {networkName} network");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new($"Node is not configured in {networkName} network");
        }

        var currency = network.Tokens.SingleOrDefault(
            x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            throw new($"Currency with asset {request.Asset} is not configured in {networkName} network");
        }

        return new()
        {
            CorrelationId = correlationId,
            Decimals = currency.Decimals,
            FromAddress = request.FromAddress,
            NodeUrl = node.Url,
            Symbol = currency.Asset,
            TokenContract = currency.TokenContract,
            CallData = request.CallData,
        };
    }

    [Activity]
    public async Task<JsTransferRequest> ExternalTransferRequestAsync(TransferRequestMessage requestMessage)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Include(x => x.ManagedAccounts)
            .SingleOrDefaultAsync(x => x.Name.ToUpper() == requestMessage.NetworkName.ToUpper());

        if (network == null)
        {
            throw new($"Chain is not configured on {requestMessage.NetworkName} network");
        }

        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);
        if (blockchainService == null)
        {
            throw new($"Blockchain service is not registerred for {network.Group}");
        }

        requestMessage.FromAddress = blockchainService.FormatAddress(requestMessage.FromAddress);
        requestMessage.ToAddress = blockchainService.FormatAddress(requestMessage.ToAddress);

        var jsTransferRequest = new JsTransferRequest
        {
            Amount = requestMessage.Amount,
            Asset = requestMessage.Asset,
            CorrelationId = requestMessage.CorrelationId,
            ReferenceId = requestMessage.ReferenceId,
            FromAddress = requestMessage.FromAddress,
            ToAddress = requestMessage.ToAddress,
            FeeAsset = requestMessage.Fee.Asset,
            FeeAmountInWei = requestMessage.Fee.AmountInWei,
            Nonce = requestMessage.Nonce,
            Network = requestMessage.NetworkName,
            CallData = requestMessage.CallData
        };

        var currency = network.Tokens.SingleOrDefault(
            x => x.Asset.ToUpper() == requestMessage.Asset.ToUpper());

        if (currency is null)
        {
            throw new($"Currency with asset {requestMessage.Asset} is not configured in {requestMessage.NetworkName} network");
        }

        Token feeCurrency = null;
        if (requestMessage.Fee.Asset is not null)
        {
            feeCurrency = network.Tokens.FirstOrDefault(
                x => x.Asset.ToUpper() == requestMessage.Fee.Asset.ToUpper());

            if (feeCurrency is null)
            {
                throw new($"Currency with asset {requestMessage.Asset} is not configured in {requestMessage.NetworkName} network");
            }
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new($"Node is not configured in {requestMessage.NetworkName} network");
        }

        jsTransferRequest.ChainId = network.ChainId;
        jsTransferRequest.TokenContract = currency.TokenContract;
        jsTransferRequest.Decimals = currency.Decimals;
        jsTransferRequest.NodeUrl = node.Url;
        jsTransferRequest.FeeTokenContract = feeCurrency?.TokenContract;

        return jsTransferRequest;
    }

    [Activity]
    public async Task<JsGetAllowanceRequest> ExternalAllowanceRequestAsync(
        string networkName,
        string owner,
        string spender,
        string asset)
    {
        var network = await dbContext.Networks
            .Include(x => x.Tokens)
            .Include(x => x.Nodes)
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network == null)
        {
            throw new($"Network  is not configured on {networkName} network");
        }

        var currency = network.Tokens.FirstOrDefault(
                       x => x.Asset.ToUpper() == asset.ToUpper());

        if (currency is null)
        {
            throw new($"Asset:{asset} is missing for network: {networkName}");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new($"Node is not configured in {networkName} network");
        }

        return new JsGetAllowanceRequest
        {
            NodeUrl = node.Url,
            OwnerAddress = owner,
            SpenderAddress = spender,
            TokenContract = currency.TokenContract,
            Decimals = currency.Decimals
        };
    }
}
