using System.Numerics;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;
using StackExchange.Redis;
using Temporalio.Activities;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Models;
using Train.Solver.Core.Models.HTLCModels;
using static Train.Solver.Core.Workflows.Helpers.ResilientNodeHelper;
using Nethereum.Web3;
using Train.Solver.Core.Services;
using Train.Solver.Blockchains.Solana.Extensions;
using Train.Solver.Blockchains.Solana.Helpers;
using Train.Solver.Blockchains.Solana.Models;
using Train.Solver.Blockchains.Solana.Programs;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Helpers;
using Train.Solver.Core.Repositories;

namespace Train.Solver.Blockchains.Solana.Activities;

public class SolanaBlockchainActivities(
    ISwapRepository swapRepository,
    INetworkRepository networkRepository,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) : BlockchainActivitiesBase(networkRepository, swapRepository), ISolanaBlockchainActivities
{
    private const int MaxConcurrentTaskCount = 4;
    private const int LamportsPerSignature = 5000;
    private const int LamportsPerRent = 3000000;
    private const int BlockhashNotFoundErrorCode = -32002;


    [Activity]
    public async Task<TransactionResponse> GetSolanaTransactionReceiptAsync(SolanaGetTransactionRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if(network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        TransactionResponse transactionReceipt;

        try
        {
            transactionReceipt = await GetTransactionAsync(request);
        }
        catch
        {
            await CheckBlockHeightAsync(network, request.FromAddress);
            throw;
        }

        var transaction = new TransactionResponse
        {
            NetworkName = request.NetworkName,
            Status = TransactionStatus.Completed,
            TransactionHash = transactionReceipt.TransactionHash,
            FeeAmount = transactionReceipt.FeeAmount,
            FeeAsset = transactionReceipt.FeeAsset,
            Timestamp = transactionReceipt.Timestamp != default
            ? transactionReceipt.Timestamp
                : DateTimeOffset.UtcNow,
            Confirmations = transactionReceipt.Confirmations,
        };

        return transaction;
    }

    [Activity(name: $"{nameof(NetworkType.Solana)}{nameof(BuildTransactionAsync)}")]
    public override async Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        PrepareTransactionResponse result;

        switch (request.Type)
        {
            case TransactionType.Transfer:
                result = await SolanaTransactionBuilder.BuildTransferTransactionAsync(network, request.Args);
                break;
            case TransactionType.HTLCLock:
                result = await SolanaTransactionBuilder.BuildHTLCLockTransactionAsync(network, request.Args);
                break;
            case TransactionType.HTLCRedeem:
                result = await SolanaTransactionBuilder.BuildHTLCRedeemTransactionAsync(network, request.Args);
                break;
            case TransactionType.HTLCRefund:
                result = await SolanaTransactionBuilder.BuildHTLCRefundTransactionAsync(network, request.Args);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.Type), $"Not supported transaction type {request.Type} for network {request.NetworkName}");
        }

        return result;
    }

    [Activity(name: $"{nameof(NetworkType.Solana)}{nameof(EstimateFeeAsync)}")]
    public override async Task<Fee> EstimateFeeAsync(EstimateFeeRequest request)
    {
        var result = new Dictionary<string, Fee>();

        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset == request.Asset);

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency), $"Currency {request.Asset} for {request.NetworkName} is missing");
        }

        var privateKeyResult = await privateKeyProvider.GetAsync(request.FromAddress);

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {network.Id} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var solanaAccount = new Account(privateKeyResult, request.FromAddress);

        var balanceForRentExemptionResult = await rpcClient.GetMinimumBalanceForRentExemptionAsync(165);

        if (!balanceForRentExemptionResult.WasSuccessful)
        {
            throw new Exception($"Failed to get minimum balance for rent exemption in network: {request.NetworkName}");
        }

        BigInteger baseFeeInLamports = balanceForRentExemptionResult.Result;
        BigInteger computeUnitsUsed = SolanaConstants.BaseLimit;

        if (request.CallData != null)
        {
            var builder = new TransactionBuilder()
                .SetFeePayer(solanaAccount);

            var signers = new List<Account> { solanaAccount };

            var transaction = Convert.FromBase64String(request.CallData);
            var tx = Solnet.Rpc.Models.Transaction.Deserialize(transaction);

            foreach (var instruction in tx.Instructions)
            {
                builder.AddInstruction(instruction);
            }

            if (tx.FeePayer != solanaAccount)
            {
                var managedAddressPrivateKey = await privateKeyProvider.GetAsync(tx.FeePayer);
                signers.Add(new Account(managedAddressPrivateKey, tx.FeePayer));
            }

            var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

            if (!latestBlockHashResponse.WasSuccessful)
            {
                throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
            }

            builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

            var rawTx = builder.Build(signers);

            var simulatedTransaction = await rpcClient.SimulateTransactionAsync(rawTx);

            if (!simulatedTransaction.WasSuccessful || simulatedTransaction.Result.Value.Error != null)
            {
                if (!simulatedTransaction.WasSuccessful)
                {
                    throw new Exception($"Failed to simulate transaction in network{request.NetworkName}: Reason {simulatedTransaction.Reason}");
                }

                throw new Exception($"Failed to simulate transaction in network{request.NetworkName}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
            }

            computeUnitsUsed += TransactionLogExtension.ExtractTotalComputeUnitsUsed(simulatedTransaction.Result.Value.Logs.ToList());

            baseFeeInLamports += ComputeRentFee(request.NetworkName, tx.Instructions) + signers.Count * LamportsPerSignature;
        }
        else
        {
            var withdrawalAmount = (ulong)Web3.Convert.ToWei(request.Amount, currency.Decimals);

            var builder = new TransactionBuilder()
                .SetFeePayer(solanaAccount);

            var transactionInstructionResult = await builder.CreateTransactionInstructionAsync(
                currency,
                rpcClient,
                solanaAccount,
                request.ToAddress,
                withdrawalAmount);

            var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

            if (!latestBlockHashResponse.WasSuccessful)
            {
                throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
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
                    new PublicKey(request.ToAddress));

                baseFeeInLamports += isActivatedAddress ?
                    0 :
                    LamportsPerRent;
            }
        }

        var nativeCurrency = network.Tokens.SingleOrDefault(x => x.TokenContract is null);

        if (nativeCurrency is null)
        {
            throw new ArgumentNullException(nameof(nativeCurrency), $"Native currency is not configured on {request.NetworkName} network");
        }

        decimal computeUnitPrice = 0;

        if (!SolanaConstants.HighComputeUnitPrice.TryGetValue(request.NetworkName, out computeUnitPrice))
        {
            throw new($"High compute unit price is not configured on {request.NetworkName} network");
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

    protected override string FormatAddress(AddressRequest request) => request.Address;

    [Activity(name: $"{nameof(NetworkType.Solana)}{nameof(GetBalanceAsync)}")]
    public override async Task<BalanceResponse> GetBalanceAsync(BalanceRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Primary node is not configured on {request.NetworkName} network");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());
        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency), $"Invalid currency");
        }

        ulong balance = default;

        try
        {
            if (currency.TokenContract is null)
            {

                var response = await rpcClient.GetBalanceAsync(request.Address);

                if (!response.WasSuccessful)
                {
                    throw new Exception("Failed to get balance");
                }

                balance = response.Result.Value;
            }
            else
            {
                var response = await rpcClient.GetTokenAccountsByOwnerAsync(request.Address, currency.TokenContract);

                if (!response.WasSuccessful)
                {
                    throw new Exception("Failed to get balance");
                }

                if (response.Result.Value.Any())
                {
                    balance = ulong.Parse(response.Result.Value.Single().Account.Data.Parsed.Info.TokenAmount.Amount);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get balance of {currency.Asset} on {request.Address} address in {request.NetworkName} network , message {ex.Message}");
        }

        var balanceResponse = new BalanceResponse
        {
            AmountInWei = balance.ToString(),
            Amount = Web3.Convert.FromWei(balance, currency.Decimals),
            Decimals = currency.Decimals,
        };

        return balanceResponse;
    }

    [Activity(name: $"{nameof(NetworkType.Solana)}{nameof(GetTransactionAsync)}")]
    public override async Task<TransactionResponse> GetTransactionAsync(GetTransactionRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var nodes = network.Nodes.Where(x => x.Type == NodeType.Primary || x.Type == NodeType.Secondary).ToList();

        if (!nodes.Any())
        {
            throw new ArgumentException($"Primary node is not configured on {request.NetworkName} network", nameof(nodes));
        }

        var epochInfoResponse = await GetDataFromNodesAsync(nodes,
            async url => await ClientFactory.GetClient(url).GetEpochInfoAsync());

        TransactionResponse transaction;
        try
        {
            transaction = await GetDataFromNodesAsync(nodes,
               async url => await GetTransactionAsync(request.TransactionId, network, epochInfoResponse.Result,
                   ClientFactory.GetClient(url)));

        }
        catch (AggregateException ae)
        {
            var transactionNotConfirmedException =
                ae.InnerExceptions.FirstOrDefault(c => c is TransactionNotComfirmedException);
            if (transactionNotConfirmedException is not null)
            {
                throw transactionNotConfirmedException;
            }

            var status = await GetDataFromNodesAsync(nodes,
                async url => await ClientFactory.GetClient(url).GetSignatureStatusAsync(request.TransactionId));

            if (status.Result.Value.FirstOrDefault() != null)
            {
                throw new TransactionNotComfirmedException($"Transaction is not confirmed yet, TxHash: {request.TransactionId}.");
            }

            throw;
        }

        if (transaction.Status == TransactionStatus.Failed)
        {
            throw new TransactionFailedException("Transaction failed");
        }

        return transaction;
    }    

   

    [Activity(name: $"{nameof(NetworkType.Solana)}{nameof(GetEventsAsync)}")]
    public override async Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Chain for network: {request.NetworkName} is not configured");
        }

        var node = network!.Nodes.FirstOrDefault(x => x.Type == NodeType.DepositTracking);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {request.NetworkName} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var blockProcessingTasks = new Dictionary<int, Task<HTLCBlockEventResponse>>();
        var blocksForProcessing = Enumerable.Range((int)request.FromBlock, (int)(request.ToBlock - request.FromBlock) + 1).ToArray();
        var events = new HTLCBlockEventResponse();

        var currencies = await networkRepository.GetTokensAsync();

        foreach (var blockChunk in blocksForProcessing.Chunk(MaxConcurrentTaskCount))
        {
            foreach (var currentBlock in blockChunk)
            {
                blockProcessingTasks[currentBlock] = EventDecoder.GetBlockEventsAsync(
                    rpcClient,
                    currentBlock,
                    network,
                    currencies);
            }

            await Task.WhenAll(blockProcessingTasks.Values);

            foreach (var blockTask in blockProcessingTasks)
            {
                events.HTLCLockEventMessages.AddRange(blockTask.Value.Result.HTLCLockEventMessages);
                events.HTLCCommitEventMessages.AddRange(blockTask.Value.Result.HTLCCommitEventMessages);
            }

            blockProcessingTasks.Clear();
        }

        return events;
    }

    [Activity(name: $"{nameof(NetworkType.Solana)}{nameof(GetLastConfirmedBlockNumberAsync)}")]
    public override async Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {request.NetworkName} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var response = await rpcClient.GetEpochInfoAsync();

        if (!response.WasSuccessful)
        {
            throw new Exception($"Failed to get epoch info");
        }

        var blockHashResponse = await rpcClient.GetBlockAsync(
            response.Result.AbsoluteSlot,
            transactionDetails: Solnet.Rpc.Types.TransactionDetailsFilterType.None);

        if (!blockHashResponse.WasSuccessful)
        {
            throw new Exception($"Failed to get block hash");
        }

        return new()
        {
            BlockNumber = response.Result.AbsoluteSlot,
            BlockHash = blockHashResponse.Result.Blockhash,
        };
    }

    public override Task<string> GetNextNonceAsync(NextNonceRequest request)
    {
        throw new NotImplementedException();
    }

    protected override async Task<string> GetCachedNonceAsync(NextNonceRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {network.Id} is not configured");
        }

        var latestBlockHashResponse = await ClientFactory
            .GetClient(node.Url)
            .GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        await cache.StringSetAsync(RedisHelper.BuildNonceKey(request.NetworkName, request.Address),
                latestBlockHashResponse.Result.Value.LastValidBlockHeight,
                expiry: TimeSpan.FromDays(7));

        return latestBlockHashResponse.Result.Value.Blockhash;
    }

    [Activity(name: $"{nameof(NetworkType.Solana)}{nameof(ValidateAddLockSignatureAsync)}")]
    public override Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request)
    {
        throw new NotImplementedException();
    }

    protected override bool ValidateAddress(AddressRequest request)
        => PublicKey.IsValid(request.Address);
 
    [Activity(name: $"{nameof(NetworkType.Solana)}{nameof(SimulateTransactionAsync)}")]
    public async Task SimulateTransactionAsync(SolanaPublishTransactionRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        if (network == null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {network.Name} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var simulatedTransaction = await rpcClient.SimulateTransactionAsync(request.RawTx);

        if (!simulatedTransaction.WasSuccessful || simulatedTransaction.Result.Value.Error != null)
        {
            if (!simulatedTransaction.WasSuccessful)
            {
                throw new Exception($"Failed to simulate transaction in network {network.Name}: Reason {simulatedTransaction.Reason}");
            }

            if (simulatedTransaction.Result.Value.Error.Type == TransactionErrorType.BlockhashNotFound)
            {
                throw new NonceMissMatchException(
                    $"Nonce mismatch error Failed to simulate transaction in network {network.Name}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
            }

            throw new Exception($"Failed to simulate transaction in network {network.Name}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
        }
    }

    [Activity(name: $"{nameof(NetworkType.Solana)}{nameof(ComposeSolanaTranscationAsync)}")]
    public async Task<byte[]> ComposeSolanaTranscationAsync(SolanaComposeTransactionRequest request)
    {
        var solanaAddress = new PublicKey(request.FromAddress);

        var builder = new TransactionBuilder()
            .SetFeePayer(solanaAddress);

        var signers = new List<string> { solanaAddress };

        if (string.IsNullOrEmpty(request.CallData))
        {
            throw new ArgumentNullException(nameof(request.CallData), "Call data is required");
        }

        var transactionBytes = Convert.FromBase64String(request.CallData);
        var tx = Solnet.Rpc.Models.Transaction.Deserialize(transactionBytes);
        foreach (var instruction in tx.Instructions)
        {
            builder.AddInstruction(instruction);
        }

        if (tx.FeePayer != solanaAddress)
        {
            signers.Add(tx.FeePayer);
        }

        builder
            .AddInstruction(ComputeBudgetProgram.SetComputeUnitLimit(uint.Parse(request.Fee.SolanaFeeData!.ComputeUnitLimit)))
            .AddInstruction(ComputeBudgetProgram.SetComputeUnitPrice((ulong)Web3.Convert.ToWei(decimal.Parse(request.Fee.SolanaFeeData.ComputeUnitPrice), SolanaConstants.MicroLamportsDecimal)))
            .SetRecentBlockHash(request.LastValidBlockHash);

        var rawTxResult = await SignSolanaTransactionAsync(builder, signers);

        return rawTxResult;
    }

    [Activity(name: $"{nameof(NetworkType.Solana)}{nameof(PublishTransactionAsync)}")]
    public async Task<string> PublishTransactionAsync(SolanaPublishTransactionRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        if (network == null)
        {
            throw new($"Network {request.NetworkName} not found");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new($"Node for network: {network.Name} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        try
        {
            var transactionResult = await rpcClient.SendSolanaTransactionAsync(request.RawTx);

            if (!transactionResult.WasSuccessful)
            {
                if (transactionResult.ServerErrorCode == BlockhashNotFoundErrorCode)
                {
                    throw new NonceMissMatchException($"Nonce miss match in network {network}, Reason: {transactionResult.RawRpcResponse}.");
                }

                throw new Exception($"Failed to submit {network.Name} transaction due to error: {transactionResult.RawRpcResponse}");
            }

            if (!string.IsNullOrEmpty(transactionResult.Result))
            {
                return transactionResult.Result;
            }
        }
        catch (Exception ex)
        {
        }

        return CalculateTransactionHash(request.RawTx);
    }

    private async Task<byte[]> SignSolanaTransactionAsync(
        TransactionBuilder builder,
        List<string> managedAddresses)
    {
        var signers = new List<Account>();

        foreach (var address in managedAddresses)
        {
            var privateKeyResult = await privateKeyProvider.GetAsync(address);

            var solanaAccount = new Account(privateKeyResult, address);
            signers.Add(solanaAccount);
        }

        return builder.Build(signers);
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
        }

        return accountCreationCount * LamportsPerRent;
    }
    private static string CalculateTransactionHash(byte[] rawTransactionBytes)
    {
        var tx = Solnet.Rpc.Models.Transaction.Deserialize(rawTransactionBytes);

        var firstSignature = tx.Signatures.First().Signature;

        var transactionHash = Encoders.Base58.EncodeData(firstSignature);

        return transactionHash;
    }

    private async Task CheckBlockHeightAsync(
     Network network,
     string fromAddress)
    {
        var primaryNode = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (primaryNode is null)
        {
            throw new ArgumentNullException(nameof(primaryNode), $"Primary node is not configured on {network.Name} network");
        }

        var primaryRpcClient = ClientFactory.GetClient(primaryNode.Url);

        var epochInfoResponseResult = await primaryRpcClient.GetEpochInfoAsync();

        if (!epochInfoResponseResult.WasSuccessful)
        {
            throw new Exception($"Failed to get latestBlock for {network.Name} network");
        }

        if (!string.IsNullOrEmpty(fromAddress))
        {
            var lastValidBlockHeight = await cache.StringGetAsync(
                RedisHelper.BuildNonceKey(network.Name, fromAddress));

            if (lastValidBlockHeight.HasValue && ulong.Parse(lastValidBlockHeight.ToString()) <= epochInfoResponseResult.Result.BlockHeight)
            {
                throw new TransactionFailedRetriableException("Transaction not found");
            }
        }
    }

    private async Task<TransactionResponse> GetTransactionAsync(
        string transactionId,
        Network network,
        EpochInfo epochInfo,
        IRpcClient rpcClient)
    {
        var feeCurrency = network.Tokens
            .Single(x => string.IsNullOrEmpty(x.TokenContract));

        var transactionReceiptResult = await rpcClient.GetParsedTransactionAsync(transactionId);

        var confirmations = (int)(epochInfo.AbsoluteSlot - (ulong)transactionReceiptResult.Result.Slot) + 1;

        if (confirmations <= 0)
        {
            throw new TransactionNotComfirmedException($"Confirmations for transaction {transactionId} in network: {network.Name} is less then 0");
        }

        var result = new TransactionResponse
        {
            TransactionHash = transactionId,
            FeeAmount = Web3.Convert.FromWei(transactionReceiptResult.Result.Meta.Fee, feeCurrency.Decimals),
            FeeAsset = feeCurrency.Asset,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(transactionReceiptResult.Result.BlockTime * 1000),
            Status = transactionReceiptResult.Result.Meta.Err is null ? TransactionStatus.Completed : TransactionStatus.Failed,
            Confirmations = confirmations,
            NetworkName = network.Name,
        };

        return result;
    }
}
