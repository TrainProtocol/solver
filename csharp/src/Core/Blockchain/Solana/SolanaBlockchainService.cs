using FluentResults;
using Microsoft.EntityFrameworkCore;
using Nethereum.Web3;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using StackExchange.Redis;
using System.Numerics;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Blockchain.Redis;
using Train.Solver.Core.Blockchain.Services;
using Train.Solver.Core.Blockchain.Solana.Extensions;
using Train.Solver.Core.Blockchain.Solana.Helpers;
using Train.Solver.Core.Blockchain.Solana.Models;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Secret;
using Train.Solver.Data;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchain.Solana;

public class SolanaBlockchainService(
    SolverDbContext dbContext,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) : BlochainServiceBase(dbContext), ISolanaBlockchainService
{
    public static NetworkGroup NetworkGroup => NetworkGroup.SOLANA;
    private const int MaxConcurrentTaskCount = 4;
    private const int LamportsPerSignature = 5000;
    private const int LamportsPerRent = 3000000;
    public override async Task<Result<PrepareTransactionResponse>> BuildTransactionAsync(string networkName, TransactionType transactionType, string args)
    {
        var network = await dbContext.Networks
            .Include(n=>n.Tokens)
            .Include(n=>n.DeployedContracts)
            .SingleOrDefaultAsync(n => n.Name == networkName);

        if (network is null)
        {
            return Result.Fail(
                new NotFoundError(
                    $"Chain is not configured for {networkName} network"));
        }
        Result<PrepareTransactionResponse> result;

        switch (transactionType)
        {
            case TransactionType.Transfer:
                result = await SolanaTransactionBuilder.BuildTransferTransactionAsync(network, args);
                break;
            case TransactionType.HTLCLock:
                result = await SolanaTransactionBuilder.BuildHTLCLockTransactionAsync(network, args);
                break;
            case TransactionType.HTLCRedeem:
                result =await  SolanaTransactionBuilder.BuildHTLCRedeemTransactionAsync(network, args);
                break;
            case TransactionType.HTLCRefund:
                result = await SolanaTransactionBuilder.BuildHTLCRefundTransactionAsync(network, args);
                break;
            default:
                return Result.Fail("Unsupported type");
        }
        return result;
    }

    public override async Task<Result<Fee>> EstimateFeeAsync(string networkName, string asset, string fromAddress, string toAddress, decimal amount, string? data = null)
    {
        var result = new Dictionary<string, Fee>();

        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail($"Chain setup for {networkName} is missing");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset == asset);

        if (currency is null)
        {
            return Result.Fail(new NotFoundError($"Currency {asset} for {networkName} is missing"));
        }

        var privateKeyResult = await privateKeyProvider.GetAsync(fromAddress);

        if (privateKeyResult.IsFailed)
        {
            return new InfinitlyRetryableError().CausedBy(privateKeyResult.Errors);
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);
        if (node is null)
        {
            return Result.Fail($"Node for network: {network.Id} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var solanaAccount = new Account(privateKeyResult.Value, fromAddress);

        var balanceForRentExemptionResult = await rpcClient.GetMinimumBalanceForRentExemptionAsync(165);

        if (!balanceForRentExemptionResult.WasSuccessful)
        {
            return Result.Fail($"Failed to get minimum balance for rent exemption in network: {networkName}");
        }

        BigInteger baseFeeInLamports = balanceForRentExemptionResult.Result;
        BigInteger computeUnitsUsed = SolanaConstants.BaseLimit;

        if (data != null)
        {
            var builder = new Solnet.Rpc.Builders.TransactionBuilder()
                .SetFeePayer(solanaAccount);

            var signers = new List<Account> { solanaAccount };

            var transaction = Convert.FromBase64String(data);
            var tx = Solnet.Rpc.Models.Transaction.Deserialize(transaction);

            foreach (var instruction in tx.Instructions)
            {
                builder.AddInstruction(instruction);
            }

            if (tx.FeePayer != solanaAccount)
            {
                var managedAddressPrivateKey = await privateKeyProvider.GetAsync(tx.FeePayer);
                signers.Add(new Account(managedAddressPrivateKey.Value, tx.FeePayer));
            }

            var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

            if (!latestBlockHashResponse.WasSuccessful)
            {
                return Result.Fail($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
            }

            builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

            var rawTx = builder.Build(signers);

            var simulatedTransaction = await rpcClient.SimulateTransactionAsync(rawTx);

            if (!simulatedTransaction.WasSuccessful || simulatedTransaction.Result.Value.Error != null)
            {
                if (!simulatedTransaction.WasSuccessful)
                {
                    return Result.Fail($"Failed to simulate transaction in network{networkName}: Reason {simulatedTransaction.Reason}");
                }

                return Result.Fail($"Failed to simulate transaction in network{networkName}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
            }

            computeUnitsUsed += TransactionLogExtension.ExtractTotalComputeUnitsUsed(simulatedTransaction.Result.Value.Logs.ToList());

            baseFeeInLamports += ComputeRentFee(networkName, tx.Instructions) + signers.Count * LamportsPerSignature;
        }
        else
        {
            var withdrawalAmount = (ulong)Web3.Convert.ToWei(amount, currency.Decimals);

            var builder = new TransactionBuilder()
                .SetFeePayer(solanaAccount);

            var transactionInstructionResult = await builder.CreateTransactionInstructionAsync(
                currency,
                rpcClient,
                solanaAccount,
                toAddress,
                withdrawalAmount);

            if (transactionInstructionResult.IsFailed)
            {
                return transactionInstructionResult.ToResult();
            }

            var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

            if (!latestBlockHashResponse.WasSuccessful)
            {
                return Result.Fail($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
            }

            builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

            var rawTx = builder.Build(solanaAccount);

            var simulatedTransaction = await rpcClient.SimulateTransactionAsync(rawTx);

            computeUnitsUsed += TransactionLogExtension.ExtractTotalComputeUnitsUsed(simulatedTransaction.Result.Value.Logs.ToList());

            baseFeeInLamports += LamportsPerSignature;

            if (currency.TokenContract != null)
            {
                var isActivatedAddress = await IsAssociatedTokenAccountInitialized(
                    rpcClient,
                    new PublicKey(currency.TokenContract),
                    new PublicKey(toAddress));

                baseFeeInLamports += isActivatedAddress ?
                    0 :
                    LamportsPerRent;
            }
        }

        var nativeCurrency = network.Tokens.SingleOrDefault(x => x.TokenContract is null);

        if (nativeCurrency is null)
        {
            return Result.Fail(
                new NotFoundError(
                      $"native currency is not configured on {networkName} network"));
        }

        decimal computeUnitPrice = 0;

        if (!SolanaConstants.HighComputeUnitPrice.TryGetValue(networkName, out computeUnitPrice))
        {
            return Result.Fail(
                new NotFoundError($"High compute unit price is not configured on {networkName} network"));
        }

        computeUnitsUsed = computeUnitsUsed.PercentageIncrease(200);
        
        return new Fee(
                nativeCurrency.Asset,
                nativeCurrency.Decimals,
                new SolanaFeeData(
                    computeUnitPrice.ToString(),
                    computeUnitsUsed.ToString(),
                    baseFeeInLamports.ToString()));
    }

    public override string FormatAddress(string address) => address;

    public override async Task<Result<string>> GenerateAddressAsync(string networkName)
    {
        var network = await dbContext.Networks
           .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Chain is not configured for {networkName} network"));
        }

        var account = new Wallet(new Mnemonic(WordList.English, WordCount.Twelve)).GetAccount(0);
        var formattedAddress = FormatAddress(account.PublicKey.Key);

        await privateKeyProvider.SetAsync(formattedAddress, account.PrivateKey);

        return Result.Ok(formattedAddress);
    }

    public override async Task<Result<BalanceResponse>> GetBalanceAsync(string networkName, string address, string asset)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Chain is not configured for {networkName} network"));
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Common node is not configured on {networkName} network"));
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == asset.ToUpper());
        if (currency is null)
        {
            return Result.Fail(new BadRequestError($"Invalid currency"));
        }

        ulong balance = default;

        try
        {
            if (currency.TokenContract is null)
            {

                var response = await rpcClient.GetBalanceAsync(address);

                if (!response.WasSuccessful)
                {
                    return Result.Fail("Failed to get balance");
                }

                balance = response.Result.Value;
            }
            else
            {
                var response = await rpcClient.GetTokenAccountsByOwnerAsync(address, currency.TokenContract);

                if (!response.WasSuccessful)
                {
                    return Result.Fail("Failed to get balance");
                }

                if (response.Result.Value.Any())
                {
                    balance = ulong.Parse(response.Result.Value.Single().Account.Data.Parsed.Info.TokenAmount.Amount);
                }
            }
        }
        catch (Exception ex)
        {
            return Result.Fail(
                new InternalError(
                    $"Failed to get balance of {currency.Asset} on {address} address in {networkName} network")
                    .CausedBy(ex));
        }

        var balanceResponse = new BalanceResponse
        {
            AmountInWei = balance.ToString(),
            Amount = Web3.Convert.FromWei(balance, currency.Decimals),
            Decimals = currency.Decimals,
        };

        return Result.Ok(balanceResponse);
    }

    public override async Task<Result<TransactionReceiptModel>> GetConfirmedTransactionAsync(string networkName, string transactionId)
    {
        var network = await dbContext.Networks
                    .Include(x => x.Nodes)
                    .Include(x => x.Tokens)
                    .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Chain is not configured on {networkName} network"));
        }

        var primaryNode = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (primaryNode is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Primary node is not configured on {networkName} network"));
        }

        var primaryRpcClient = ClientFactory.GetClient(primaryNode.Url);

        var epochInfoResponseResult = await primaryRpcClient.GetEpochInfoAsync();

        if (!epochInfoResponseResult.WasSuccessful)
        {
            return Result.Fail($"Failed to get latestBlock for {networkName} network");
        }

        var getTransactionTasks = new List<Task<Result<TransactionReceiptModel>>>
        {
           GetTransactionAsync(transactionId, network, epochInfoResponseResult.Result, primaryRpcClient)
        };

        var secondaryNode = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Secondary);

        if (secondaryNode is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Secondary node is not configured on {networkName} network"));
        }

        var secondaryRpcClient = ClientFactory.GetClient(secondaryNode.Url);
        getTransactionTasks.Add(GetTransactionAsync(transactionId, network, epochInfoResponseResult.Result, secondaryRpcClient));

        var completedTasks = Task.WhenAll(getTransactionTasks);
        var results = await completedTasks;

        if (results.All(x => x.IsFailed))
        {
            foreach (var result in results)
            {
                var errorMessage = result.Errors.First().Message;
                Serilog.Log.Information(errorMessage);
            }

            if (results.All(x => x.HasError<NodeError>()))
            {
                return Result.Fail(
                    new NodeError(
                        $"Get Transaction receipt failed due to {string.Join(",", results.Select(x => x.Errors.FirstOrDefault()!.Message))}"));
            }
            else if (results.All(x => x.HasError<TransactionNotConfirmedError>()))
            {
                return results.First();
            }
            else
            {
                var getStatusTasks = new List<Task<Result<SolanaSignatureStatusResponse>>>
                {
                    primaryRpcClient.GetSignatureStatusAsync(transactionId),
                    secondaryRpcClient.GetSignatureStatusAsync(transactionId),
                };

                var completedStatusTasks = Task.WhenAll(getStatusTasks);

                var statusResults = await completedStatusTasks;

                if (statusResults.All(x => x.IsSuccess))
                {
                    foreach (var statusResult in statusResults)
                    {
                        if (statusResult.Value.Result.Value.FirstOrDefault() != null)
                        {
                            return Result.Fail(
                                new TransactionNotConfirmedError($"Transaction is not confirmed yet, TxHash: {transactionId}."));
                        }
                    }
                }
                else if (statusResults.All(x => x.HasError<NodeError>()))
                {
                    return Result.Fail(
                        new NodeError(
                            $"Get Transaction status failed due to {string.Join(",", results.Select(x => x.Errors.FirstOrDefault()!.Message))}"));
                }

                return results.First().ToResult();
            }
        }

        var response = results.FirstOrDefault(x => x.IsSuccess)!.Value;

        if (response.Status == TransactionStatuses.Failed)
        {
            return Result.Fail(new TransactionFailedError("Transaction failed"));
        }

        return Result.Ok(response);
    }

    private async Task<Result<TransactionReceiptModel>> GetTransactionAsync(
        string transactionId,
        Network network,
        EpochInfo epochInfo,
        IRpcClient rpcClient)
    {
        var feeCurrency = network.Tokens
            .SingleOrDefault(x => string.IsNullOrEmpty(x.TokenContract));

        if (feeCurrency is null)
        {
            return Result.Fail($"Failed to get fee currency {network.Name}");
        }

        var transactionReceiptResult = await rpcClient.GetParsedTransactionAsync(transactionId);

        if (transactionReceiptResult.IsFailed)
        {
            return transactionReceiptResult.ToResult();
        }

        var confirmations = (int)(epochInfo.AbsoluteSlot - (ulong)transactionReceiptResult.Value.Result.Slot) + 1;

        if (confirmations <= 0)
        {
            return Result.Fail(
                new TransactionNotConfirmedError(
                    $"Confirmations for transaction {transactionId} in network: {network.Name} is less then 0"));
        }

        var result = new TransactionReceiptModel
        {
            TransactionId = transactionId,
            FeeAmount = Web3.Convert.FromWei(transactionReceiptResult.Value.Result.Meta.Fee, feeCurrency.Decimals),
            FeeAmountInWei = transactionReceiptResult.Value.Result.Meta.Fee.ToString(),
            FeeDecimals = feeCurrency.Decimals,
            FeeAsset = feeCurrency.Asset,
            BlockNumber = transactionReceiptResult.Value.Result.Slot,
            Timestamp = transactionReceiptResult.Value.Result.BlockTime * 1000,
            Status = transactionReceiptResult.Value.Result.Meta.Err is null ? TransactionStatuses.Completed : TransactionStatuses.Failed,
            Confirmations = confirmations,
        };

        return Result.Ok(result);
    }

    public override async Task<Result<HTLCBlockEvent>> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Include(x => x.ManagedAccounts)
            .Include(x => x.DeployedContracts)
            .FirstOrDefaultAsync(x =>
                x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail($"Chain for network: {networkName} is not configured");
        }

        var node = network!.Nodes.FirstOrDefault(x => x.Type == NodeType.DepositTracking);

        if (node is null)
        {
            return Result.Fail($"Node for network: {networkName} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var blockProcessingTasks = new Dictionary<int, Task<Result<HTLCBlockEvent>>>();
        var blocksForProcessing = Enumerable.Range((int)fromBlock, (int)(toBlock - fromBlock) + 1).ToArray();
        var events = new HTLCBlockEvent();

        var currencies = await dbContext.Tokens
           .Include(x => x.Network)
           .ToListAsync();

        foreach (var blockChunk in blocksForProcessing.Chunk(MaxConcurrentTaskCount))
        {
            foreach (var currentBlock in blockChunk)
            {
                blockProcessingTasks[currentBlock] = Helpers.EventDecoder.GetBlockEventsAsync(
                    rpcClient,
                    currentBlock,
                    network,
                    currencies);
            }

            await Task.WhenAll(blockProcessingTasks.Values);

            foreach (var blockTask in blockProcessingTasks)
            {
                var result = await blockTask.Value;

                if (result.IsFailed)
                {
                    return result.ToResult();
                }
                else
                {
                    events.HTLCLockEventMessages.AddRange(result.Value.HTLCLockEventMessages);
                    events.HTLCCommitEventMessages.AddRange(result.Value.HTLCCommitEventMessages);
                }
            }

            blockProcessingTasks.Clear();
        }

        return events;
    }

    public override async Task<Result<BlockNumberResponse>> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        var network = await dbContext.Networks
                          .Include(x => x.Nodes)
                          .AsNoTracking()
                          .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail($"Chain for network: {networkName} is not configured");
        }

        var node = network!.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            return Result.Fail($"Node for network: {networkName} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var response = await rpcClient.GetEpochInfoAsync();

        if (!response.WasSuccessful)
        {
            return Result.Fail($"Failed to get epoch info");
        }

        var blockHashResponse = await rpcClient.GetBlockAsync(
            response.Result.AbsoluteSlot,
            transactionDetails: Solnet.Rpc.Types.TransactionDetailsFilterType.None);

        if (!blockHashResponse.WasSuccessful)
        {
            return Result.Fail($"Failed to get block hash");
        }

        return Result.Ok(new BlockNumberResponse
        {
            BlockNumber = response.Result.AbsoluteSlot.ToString(),
            BlockHash = blockHashResponse.Result.Blockhash,
        });
    }

    public override async Task<Result<string>> GetNextNonceAsync(string networkName, string address, string referenceId)
    {
        var network = await dbContext.Networks
                   .Include(x => x.Nodes)
                   .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail($"Chain setup for {networkName} is missing");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);
        if (node is null)
        {
            return Result.Fail($"Node for network: {network.Id} is not configured");
        }

        var latestBlockHashResponse = await ClientFactory
            .GetClient(node.Url)
            .GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            return Result.Fail($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        await cache.StringSetAsync(RedisHelper.BuildNonceKey(networkName, address),
                latestBlockHashResponse.Result.Value.LastValidBlockHeight,
                expiry: TimeSpan.FromDays(7));

        return Result.Ok(latestBlockHashResponse.Result.Value.Blockhash);
    }

    public override Task<Result<string>> GetSpenderAllowanceAsync(string networkName, string ownerAddress, string spenderAddress, string asset)
    {
        throw new NotImplementedException();
    }

    public override Task<Result<bool>> ValidateAddLockSignatureAsync(string networkName, AddLockSigValidateRequest request)
    {
        throw new NotImplementedException();
    }

    public override bool ValidateAddress(string address) 
        => PublicKey.IsValid(address);

    private static BigInteger ComputeRentFee(
       string networkName,
       List<TransactionInstruction> instructions)
    {
        int accountCreationCount = 0;

        foreach (var instruction in instructions)
        {
            if (instruction.ProgramId.SequenceEqual(AssociatedTokenAccountProgram.ProgramIdKey.KeyBytes) && instruction.Data.Length == 0)
            {
                accountCreationCount++;
            }
            if (SolanaConstants.LockDescriminator.TryGetValue(networkName, out var lockDescriminator) && instruction.Data.Take(8).SequenceEqual(lockDescriminator))
            {
                accountCreationCount++;
            }
            if (instruction.Data.Take(8).SequenceEqual(FieldEncoder.Sighash(SolanaConstants.depositForBurnSighash)))
            {
                accountCreationCount++;
            }
        }

        return accountCreationCount * LamportsPerRent;
    }

    private static async Task<bool> IsAssociatedTokenAccountInitialized(
        IRpcClient rpcClient,
        PublicKey mintAddress,
        PublicKey solAddress)
    {
        var splAddress = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(solAddress, mintAddress);
        var accountInfoResult = await rpcClient.GetAccountInfoAsync(splAddress);

        if (accountInfoResult.WasSuccessful && accountInfoResult.Result.Value != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public virtual async Task<Result<BaseError>> CheckBlockHeightAsync(
        Network network,
        string fromAddress)
    {
        var primaryNode = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (primaryNode is null)
        {
            return new NotFoundError($"Primary node is not configured on {network.Name} network");
        }

        var primaryRpcClient = ClientFactory.GetClient(primaryNode.Url);

        var epochInfoResponseResult = await primaryRpcClient.GetEpochInfoAsync();

        if (!epochInfoResponseResult.WasSuccessful)
        {
            throw new($"Failed to get latestBlock for {network.Name} network");
        }

        if (!string.IsNullOrEmpty(fromAddress))
        {
            var lastValidBlockHeight = await cache.StringGetAsync(
                RedisHelper.BuildNonceKey(network.Name, fromAddress));

            if (lastValidBlockHeight.HasValue && ulong.Parse(lastValidBlockHeight.ToString()) <= epochInfoResponseResult.Result.BlockHeight)
            {
                return new TransactionFailedRetryableError($"Transaction not found");
            }
        }

        return default;
    }
}
