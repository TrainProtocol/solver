using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Nethereum.Web3;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using Solnet.Wallet.Utilities;
using StackExchange.Redis;
using Temporalio.Activities;
using Train.Solver.Core.Activities;
using Train.Solver.Core.Blockchains.Solana.Extensions;
using Train.Solver.Core.Blockchains.Solana.Helpers;
using Train.Solver.Core.Blockchains.Solana.Programs;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Helpers;
using Train.Solver.Core.Models;
using Train.Solver.Core.Services.Secret;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using static Train.Solver.Core.Helpers.ResilientNodeHelper;

namespace Train.Solver.Core.Blockchains.Solana.Activities;

public class SolanaBlockchainActivities(
    SolverDbContext dbContext,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) : BlockchainActivitiesBase(dbContext), ISolanaBlockchainActivities
{
    public static NetworkGroup NetworkGroup => NetworkGroup.Solana;
    private const int MaxConcurrentTaskCount = 4;
    private const int LamportsPerSignature = 5000;
    private const int LamportsPerRent = 3000000;
    private const int BlockhashNotFoundErrorCode = -32002;


    [Activity]
    public async Task<TransactionModel> GetSolanaTransactionReceiptAsync(
        string networkName,
        string fromAddress,
        string transactionHash)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .SingleAsync();
        
        TransactionModel transactionReceipt;

        try
        {
            transactionReceipt = await GetTransactionAsync(
                networkName,
                transactionHash);
        }
        catch
        {
            await CheckBlockHeightAsync(network, fromAddress);
            throw;
        }

        var transaction = new TransactionModel
        {
            NetworkName = networkName,
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

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(BuildTransactionAsync)}")]
    public override async Task<PrepareTransactionResponse> BuildTransactionAsync(string networkName, TransactionType transactionType, string args)
    {
        var network = await dbContext.Networks
            .Include(n => n.Tokens)
            .Include(n => n.Nodes)
            .Include(n => n.ManagedAccounts)
            .Include(n => n.DeployedContracts)
            .SingleAsync(n => n.Name == networkName);

        PrepareTransactionResponse result;

        switch (transactionType)
        {
            case TransactionType.Transfer:
                result = await SolanaTransactionBuilder.BuildTransferTransactionAsync(network, args);
                break;
            case TransactionType.HTLCLock:
                result = await SolanaTransactionBuilder.BuildHTLCLockTransactionAsync(network, args);
                break;
            case TransactionType.HTLCRedeem:
                result = await SolanaTransactionBuilder.BuildHTLCRedeemTransactionAsync(network, args);
                break;
            case TransactionType.HTLCRefund:
                result = await SolanaTransactionBuilder.BuildHTLCRefundTransactionAsync(network, args);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(transactionType), $"Not supported transaction type {transactionType} for network {networkName}");
        }

        return result;
    }

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(EstimateFeeAsync)}")]
    public override async Task<Fee> EstimateFeeAsync(string networkName, EstimateFeeRequest request)
    {
        var result = new Dictionary<string, Fee>();

        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var currency = network.Tokens.SingleOrDefault(x => x.Asset == request.Asset);

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency), $"Currency {request.Asset} for {networkName} is missing");
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
            throw new Exception($"Failed to get minimum balance for rent exemption in network: {networkName}");
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
                    throw new Exception($"Failed to simulate transaction in network{networkName}: Reason {simulatedTransaction.Reason}");
                }

                throw new Exception($"Failed to simulate transaction in network{networkName}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
            }

            computeUnitsUsed += TransactionLogExtension.ExtractTotalComputeUnitsUsed(simulatedTransaction.Result.Value.Logs.ToList());

            baseFeeInLamports += ComputeRentFee(networkName, tx.Instructions) + signers.Count * LamportsPerSignature;
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
            throw new ArgumentNullException(nameof(nativeCurrency), $"Native currency is not configured on {networkName} network");
        }

        decimal computeUnitPrice = 0;

        if (!SolanaConstants.HighComputeUnitPrice.TryGetValue(networkName, out computeUnitPrice))
        {
            throw new($"High compute unit price is not configured on {networkName} network");
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

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(FormatAddress)}")]
    public override string FormatAddress(string address) => address;

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(GenerateAddressAsync)}")]
    public override async Task<string> GenerateAddressAsync(string networkName)
    {
        var network = await dbContext.Networks
           .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            throw new($"Chain is not configured for {networkName} network");
        }

        var account = new Wallet(new Mnemonic(WordList.English, WordCount.Twelve)).GetAccount(0);
        var formattedAddress = FormatAddress(account.PublicKey.Key);

        await privateKeyProvider.SetAsync(formattedAddress, account.PrivateKey);

        return formattedAddress;
    }

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(GetBalanceAsync)}")]
    public override async Task<BalanceModel> GetBalanceAsync(string networkName, string address, string asset)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Primary node is not configured on {networkName} network");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == asset.ToUpper());
        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency), $"Invalid currency");
        }

        ulong balance = default;

        try
        {
            if (currency.TokenContract is null)
            {

                var response = await rpcClient.GetBalanceAsync(address);

                if (!response.WasSuccessful)
                {
                    throw new Exception("Failed to get balance");
                }

                balance = response.Result.Value;
            }
            else
            {
                var response = await rpcClient.GetTokenAccountsByOwnerAsync(address, currency.TokenContract);

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
            throw new Exception($"Failed to get balance of {currency.Asset} on {address} address in {networkName} network , message {ex.Message}");
        }

        var balanceResponse = new BalanceModel
        {
            AmountInWei = balance.ToString(),
            Amount = Web3.Convert.FromWei(balance, currency.Decimals),
            Decimals = currency.Decimals,
        };

        return balanceResponse;
    }

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(GetTransactionAsync)}")]
    public override async Task<TransactionModel> GetTransactionAsync(string networkName, string transactionId)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var nodes = network.Nodes.Where(x => x.Type == NodeType.Primary || x.Type == NodeType.Secondary).ToList();

        if (!nodes.Any())
        {
            throw new ArgumentException($"Primary node is not configured on {networkName} network", nameof(nodes));
        }

        var epochInfoResponse = await GetDataFromNodesAsync(nodes,
            async url => await ClientFactory.GetClient(url).GetEpochInfoAsync());

        TransactionModel transaction;
        try
        {
            transaction = await GetDataFromNodesAsync(nodes,
               async url => await GetTransactionAsync(transactionId, network, epochInfoResponse.Result,
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
                async url => await ClientFactory.GetClient(url).GetSignatureStatusAsync(transactionId));

            if (status.Result.Value.FirstOrDefault() != null)
            {
                throw new TransactionNotComfirmedException($"Transaction is not confirmed yet, TxHash: {transactionId}.");
            }

            throw;
        }

        if (transaction.Status == TransactionStatus.Failed)
        {
            throw new TransactionFailedException("Transaction failed");
        }

        return transaction;
    }

    private async Task<TransactionModel> GetTransactionAsync(
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

        var result = new TransactionModel
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

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(GetEventsAsync)}")]
    public override async Task<HTLCBlockEvent> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock)
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
            throw new ArgumentNullException(nameof(network), $"Chain for network: {networkName} is not configured");
        }

        var node = network!.Nodes.FirstOrDefault(x => x.Type == NodeType.DepositTracking);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {networkName} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var blockProcessingTasks = new Dictionary<int, Task<HTLCBlockEvent>>();
        var blocksForProcessing = Enumerable.Range((int)fromBlock, (int)(toBlock - fromBlock) + 1).ToArray();
        var events = new HTLCBlockEvent();

        var currencies = await dbContext.Tokens
           .Include(x => x.Network)
           .ToListAsync();

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

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(GetLastConfirmedBlockNumberAsync)}")]
    public override async Task<BlockNumberModel> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .AsNoTracking()
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {networkName} is not configured");
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

    public override Task<string> GetNextNonceAsync(string networkName, string address)
    {
        throw new NotImplementedException();
    }

    protected override async Task<string> GetPersistedNonceAsync(string networkName, string address)
    {
        var network = await dbContext.Networks
                   .Include(x => x.Nodes)
                   .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

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

        await cache.StringSetAsync(RedisHelper.BuildNonceKey(networkName, address),
                latestBlockHashResponse.Result.Value.LastValidBlockHeight,
                expiry: TimeSpan.FromDays(7));

        return latestBlockHashResponse.Result.Value.Blockhash;
    }

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(GetReservedNonceAsync)}")]
    public override Task<string> GetReservedNonceAsync(string networkName, string address, string referenceId)
    {
        return base.GetReservedNonceAsync(networkName, address, referenceId);
    }

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(ValidateAddLockSignatureAsync)}")]
    public override Task<bool> ValidateAddLockSignatureAsync(string networkName, AddLockSignatureRequest request)
    {
        throw new NotImplementedException();
    }

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(ValidateAddress)}")]
    public override bool ValidateAddress(string address)
        => PublicKey.IsValid(address);
 
    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(SimulateTransactionAsync)}")]
    public async Task SimulateTransactionAsync(string networkName, byte[] rawTx)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network == null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {networkName} not found");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {network.Name} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var simulatedTransaction = await rpcClient.SimulateTransactionAsync(rawTx);

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

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(ComposeSolanaTranscationAsync)}")]
    public async Task<byte[]> ComposeSolanaTranscationAsync(Fee fee, string fromAddress, string callData, string lastValidBLockHash)
    {
        var solanaAddress = new PublicKey(fromAddress);

        var builder = new TransactionBuilder()
            .SetFeePayer(solanaAddress);

        var signers = new List<string> { solanaAddress };

        if (string.IsNullOrEmpty(callData))
        {
            throw new ArgumentNullException(nameof(callData), "Call data is required");
        }

        var transactionBytes = Convert.FromBase64String(callData);
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
            .AddInstruction(ComputeBudgetProgram.SetComputeUnitLimit(uint.Parse(fee.SolanaFeeData!.ComputeUnitLimit)))
            .AddInstruction(ComputeBudgetProgram.SetComputeUnitPrice((ulong)Web3.Convert.ToWei(decimal.Parse(fee.SolanaFeeData.ComputeUnitPrice), SolanaConstants.MicroLamportsDecimal)))
            .SetRecentBlockHash(lastValidBLockHash);

        var rawTxResult = await SignSolanaTransactionAsync(builder, signers);

        return rawTxResult;
    }

    [Activity(name: $"{nameof(NetworkGroup.Solana)}{nameof(PublishTransactionAsync)}")]
    public async Task<string> PublishTransactionAsync(string networkName, byte[] rawTx)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network == null)
        {
            throw new($"Network {networkName} not found");
        }

        var node = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new($"Node for network: {network.Name} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        try
        {
            var transactionResult = await rpcClient.SendSolanaTransactionAsync(rawTx);

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
            Serilog.Log.Error($"Occurred exception while sending raw transaction in network {network.Name}. Message {ex.Message}.");
        }

        return CalculateTransactionHash(rawTx);
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

}

