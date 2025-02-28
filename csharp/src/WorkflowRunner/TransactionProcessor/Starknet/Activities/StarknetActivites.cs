using FluentResults;
using Microsoft.EntityFrameworkCore;
using Nethereum.Web3;
using Temporalio.Activities;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Temporal.Abstractions.Models;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using Train.Solver.WorkflowRunner.Exceptions;
using Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Models;

namespace Train.Solver.WorkflowRunner.TransactionProcessor.Starknet.Activities;

public class StarknetActivites(
    SolverDbContext dbContext,
    IKeyedServiceProvider serviceProvider)
{
    [Activity]
    public async Task<object> HTLCTransactionRequestBuilder(string networkName, TransactionType type, string args)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Include(x => x.DeployedContracts)
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network == null)
        {
            throw new($"Network {networkName} not found");
        }

        

        switch (type)
        {
            case TransactionType.HTLCLock:
                {
                    var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);

                    if(blockchainService is not null)
                    {
                        return LockTransactionBuilder(blockchainService, args, network);
                    }
                    return Result.Fail($"Blockchain Service is not configured for {network.Group}");

                }
            case TransactionType.HTLCRedeem:
                {
                    return RedeemTransactionBuilder(args, network);
                }
            case TransactionType.HTLCRefund:
                {
                    return RefundTransactionBuilder(args, network);
                }
            case TransactionType.Approve:
                {
                    return ApproveTransactionBuilder(args, network);
                }
            case TransactionType.HTLCAddLockSig:
                {
                    return await LockSigTransactionBuilder(args, network);
                }
            default:
                {
                    throw new($"Transaction type {type} is not supported");
                }
        }
    }

    [Activity]
    public async Task<TransactionModel> GetStarknetTransactionReceiptAsync(
        string networkName,
        IEnumerable<string> transactionIds)
    {
        var network = await dbContext.Networks
            .Include(x => x.Tokens)
            .Include(x => x.DeployedContracts)
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName);

        if (network == null)
        {
            throw new("Invalid network");
        }
        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);

        if (blockchainService is null)
        {
            throw new($"Transaction receipt provider is not registered for {network.Group}");
        }

        TransactionReceiptModel? receipt = null;

        foreach (var transactionId in transactionIds)
        {
            var result = await blockchainService.GetConfirmedTransactionAsync(networkName, transactionId);

            if (result.IsSuccess)
            {
                receipt = result.Value;
            }
        }

        if (receipt == null)
        {
            throw new TransactionNotComfirmedException("Transaction not confirmed");
        }

        if (receipt.Status != TransactionStatuses.Completed)
        {
            throw new("Transaction failed");
        }

        var transaction = new TransactionModel
        {
            NetworkName = networkName,
            Status = TransactionStatus.Completed,
            TransactionHash = receipt.TransactionId,
            FeeAmount = receipt.FeeAmount,
            FeeAsset = receipt.FeeAsset,
            Timestamp = receipt.Timestamp != default
                ? DateTimeOffset.FromUnixTimeMilliseconds(receipt.Timestamp)
                : DateTimeOffset.UtcNow,
            Confirmations = receipt.Confirmations
        };

        return transaction;
    }

    [Activity]
    public async Task<string> GetStarknetSpenderAddressAsync(string networkName)
    {
        var network = await dbContext.Networks
            .Include(x => x.DeployedContracts)
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network == null)
        {
            throw new($"Network {networkName} not found");
        }

        var htlcContractAddress = network.DeployedContracts.FirstOrDefault(c => c.Type == ContarctType.HTLCTokenContractAddress);

        if (htlcContractAddress == null)
        {
            throw new($"HTLC contract address for {networkName} is missing");
        }

        return htlcContractAddress.Address;
    }

    private static JsHTLCLockTransactionBuilderRequest LockTransactionBuilder(
        IBlockchainService blockchainService,
        string args,
        Network network)
    {
        var request = args.FromArgs<HTLCLockTransactionPrepareRequest>();
        request.Receiver = blockchainService.FormatAddress(request.Receiver);

        var jsHTLCLockTransactionBuilderRequest = new JsHTLCLockTransactionBuilderRequest
        {
            FunctionName = FunctionName.Lock
        };

        var token = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.SourceAsset.ToUpper());

        if (token is null)
        {
            throw new($"Currency {request.SourceAsset} for {network.Name} is missing");
        }

        var htlcContract = network.DeployedContracts.FirstOrDefault(c => c.Type == ContarctType.HTLCTokenContractAddress);

        if (htlcContract is null)
        {
            throw new($"HTLC contract address for {network.Name} is missing");
        }

        jsHTLCLockTransactionBuilderRequest.Timelock = request.Timelock.ToString();
        jsHTLCLockTransactionBuilderRequest.Hashlock = request.Hashlock;
        jsHTLCLockTransactionBuilderRequest.AmountInWei = Web3.Convert.ToWei(request.Amount, token.Decimals).ToString();
        jsHTLCLockTransactionBuilderRequest.RewardInWei = Web3.Convert.ToWei(request.Reward, token.Decimals).ToString();
        jsHTLCLockTransactionBuilderRequest.RewardTimelock = request.RewardTimelock.ToString();
        jsHTLCLockTransactionBuilderRequest.Reward = request.Reward;
        jsHTLCLockTransactionBuilderRequest.Amount = request.Amount;
        jsHTLCLockTransactionBuilderRequest.Receiver = request.Receiver;
        jsHTLCLockTransactionBuilderRequest.SourceAsset = request.SourceAsset;
        jsHTLCLockTransactionBuilderRequest.DestinationChain = request.DestinationNetwork;
        jsHTLCLockTransactionBuilderRequest.DestinationAddress = request.DestinationAddress;
        jsHTLCLockTransactionBuilderRequest.DestinationAsset = request.DestinationAsset;
        jsHTLCLockTransactionBuilderRequest.Id = request.Id;
        jsHTLCLockTransactionBuilderRequest.TokenContract = token.TokenContract;
        jsHTLCLockTransactionBuilderRequest.ContractAddress = htlcContract.Address;
        jsHTLCLockTransactionBuilderRequest.IsErc20 = token.TokenContract != null;

        return jsHTLCLockTransactionBuilderRequest;
    }

    private static JsHTLCRedeemTransactionBuilderRequest RedeemTransactionBuilder(
        string args,
        Network network)
    {
        var request = args.FromArgs<HTLCRedeemTransactionPrepareRequest>();

        var jsHTLCRedeemTransactionBuilderRequest = new JsHTLCRedeemTransactionBuilderRequest
        {
            FunctionName = FunctionName.Redeem
        };

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            throw new($"Currency {request.Asset} for {network.Name} is missing");
        };

        var htlcContract = network.DeployedContracts.FirstOrDefault(c => c.Type == ContarctType.HTLCTokenContractAddress);

        if (htlcContract is null)
        {
            throw new($"HTLC contract address for {network.Name} is missing");
        }

        jsHTLCRedeemTransactionBuilderRequest.IsErc20 = currency.TokenContract != null;
        jsHTLCRedeemTransactionBuilderRequest.ContractAddress = htlcContract.Address;
        jsHTLCRedeemTransactionBuilderRequest.Secret = request.Secret;
        jsHTLCRedeemTransactionBuilderRequest.Id = request.Id;

        return jsHTLCRedeemTransactionBuilderRequest;
    }

    private static JsHTLCRefundTransactionBuilderRequest RefundTransactionBuilder(
        string args,
        Network network)
    {
        var request = args.FromArgs<HTLCRefundTransactionPrepareRequest>();

        var jsHTLCRefundTransactionBuilderRequest = new JsHTLCRefundTransactionBuilderRequest
        {
            FunctionName = FunctionName.Refund
        };

        var currency = network.Tokens.FirstOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            throw new($"Currency {request.Asset} for {network.Name} is missing");
        };

        var htlcContract = network.DeployedContracts.FirstOrDefault(c => c.Type == ContarctType.HTLCTokenContractAddress);

        if (htlcContract is null)
        {
            throw new($"HTLC contract address for {network.Name} is missing");
        }

        jsHTLCRefundTransactionBuilderRequest.IsErc20 = currency.TokenContract != null;
        jsHTLCRefundTransactionBuilderRequest.ContractAddress = htlcContract.Address;
        jsHTLCRefundTransactionBuilderRequest.Id = request.Id;

        return jsHTLCRefundTransactionBuilderRequest;
    }

    private JsApproveTransactionBuilderRequest ApproveTransactionBuilder(
        string args,
        Network network)
    {
        var reqeust = args.FromArgs<ApprovePrepareRequest>();
        var blockChainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);

        if(blockChainService is null)
        {
            throw new($"Blockchain service is not configured for {network.Name} network");
        }

        reqeust.SpenderAddress = blockChainService.FormatAddress(reqeust.SpenderAddress);

        var jsApproveTransactionBuilderRequest = new JsApproveTransactionBuilderRequest
        {
            Spender = reqeust.SpenderAddress,
            FunctionName = FunctionName.Approve
        };

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == reqeust.Asset.ToUpper());

        if (currency is null)
        {
            throw new($"Currency {reqeust.Asset} for {network.Name} is missing");
        }

        if (string.IsNullOrEmpty(currency.TokenContract))
        {
            throw new($"Currency {reqeust.Asset} for {network.Name} is not an ERC20 token");
        }

        jsApproveTransactionBuilderRequest.ContractAddress = currency.TokenContract;
        jsApproveTransactionBuilderRequest.TokenContract = currency.TokenContract;
        jsApproveTransactionBuilderRequest.AmountInWei = Web3.Convert.ToWei(reqeust.Amount, currency.Decimals).ToString();

        return jsApproveTransactionBuilderRequest;
    }

    private async Task<JsHTLCAddLockSigValidateRequest> LockSigTransactionBuilder(
        string args,
        Network network)
    {
        var reqeust = args.FromArgs<HTLCAddLockSigTransactionPrepareRequest>();

        var swap = await dbContext.Swaps
            .Include(x => x.SourceToken.Network)
            .FirstOrDefaultAsync(x => x.Id == reqeust.Id);

        if (swap is null)
        {
            throw new($"Swap not found");
        }
        var blockchainService = serviceProvider.GetKeyedService<IBlockchainService>(network.Group);
        var signerAddress = blockchainService.FormatAddress(swap.SourceAddress);

        if (reqeust.SignatureArray is null)
        {
            throw new("Signature is missing");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == reqeust.Asset.ToUpper());

        if (currency is null)
        {
            throw new($"Currency {reqeust.Asset} for {network.Name} is missing");
        }

        var htlcContract = network.DeployedContracts.FirstOrDefault(c => c.Type == ContarctType.HTLCTokenContractAddress);

        if (htlcContract is null)
        {
            throw new($"HTLC contract address for {network.Name} is missing");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new($"Primary node for {network.Name} is missing");
        }

        var jsHTLCAddLockSigValidateRequest = new JsHTLCAddLockSigValidateRequest
        {
            Hashlock = reqeust.Hashlock,
            Timelock = reqeust.Timelock.ToString(),
            SignatureArray = reqeust.SignatureArray,
            SignerAddress = signerAddress,
            FunctionName = FunctionName.AddLockSig,
            ContractAddress = htlcContract.Address,
            ChainId = network.ChainId,
            NodeUrl = node.Url,
            Id = reqeust.Id
        };

        jsHTLCAddLockSigValidateRequest.IsErc20 = currency.TokenContract != null;


        return jsHTLCAddLockSigValidateRequest;
    }
}
