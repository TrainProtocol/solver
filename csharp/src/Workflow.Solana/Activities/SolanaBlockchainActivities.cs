using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;
using System.Buffers;
using System.Numerics;
using Temporalio.Activities;
using Train.Solver.Blockchain.Solana.Extensions;
using Train.Solver.Blockchain.Solana.Helpers;
using Train.Solver.Blockchain.Solana.Models;
using Train.Solver.Blockchain.Solana.Programs;
using Train.Solver.Common.Enums;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.Solana.Helpers;
using Train.Solver.Workflow.Solana.Models;
using Transaction = Solnet.Rpc.Models.Transaction;

namespace Train.Solver.Workflow.Solana.Activities;

public class SolanaBlockchainActivities(
    IPrivateKeyProvider privateKeyProvider) : ISolanaBlockchainActivities, IBlockchainActivities 
{
    private const int MaxConcurrentTaskCount = 4;
    private const int LamportsPerRent = 3000000;
    private const int BlockhashNotFoundErrorCode = -32002;

    [Activity]
    public virtual async Task<PrepareTransactionDto> BuildTransactionAsync(TransactionBuilderRequest request)
    {
        PrepareTransactionDto result;

        switch (request.Type)
        {
            case TransactionType.Transfer:
                result = await SolanaTransactionBuilder.BuildTransferTransactionAsync(request.Network, request.PrepareArgs);
                break;
            case TransactionType.HTLCLock:
                result = await SolanaTransactionBuilder.BuildHTLCLockTransactionAsync(request.Network, request.FromAddress!, request.PrepareArgs);
                break;
            case TransactionType.HTLCRedeem:
                result = await SolanaTransactionBuilder.BuildHTLCRedeemTransactionAsync(request.Network, request.FromAddress!, request.PrepareArgs);
                break;
            case TransactionType.HTLCRefund:
                result = await SolanaTransactionBuilder.BuildHTLCRefundTransactionAsync(request.Network, request.FromAddress!, request.PrepareArgs);
                break;
            case TransactionType.HTLCAddLockSig:
                result = await SolanaTransactionBuilder.BuildHTLCAddlockSigTransactionAsync(request.Network, request.FromAddress!, request.PrepareArgs);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.Type), $"Not supported transaction type {request.Type} for network {request.Network.Name}");
        }

        return result;
    }

    [Activity]
    public virtual async Task<BalanceResponse> GetBalanceAsync(BalanceRequest request)
    {
        var node = request.Network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Primary node is not configured on {request.Network.Name} network");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var currency = request.Network.Tokens.SingleOrDefault(x => x.Symbol.ToUpper() == request.Asset.ToUpper());
        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency), $"Invalid currency");
        }

        ulong balance = default;

        try
        {
            if (currency.Contract is null)
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
                var response = await rpcClient.GetTokenAccountsByOwnerAsync(request.Address, currency.Contract);

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
            throw new Exception($"Failed to get balance of {currency.Symbol} on {request.Address} address in {request.Network.Name} network , message {ex.Message}");
        }

        var balanceResponse = new BalanceResponse
        {
            Amount = balance,
        };

        return balanceResponse;
    }

    [Activity]
    public virtual async Task<TransactionResponse> GetTransactionAsync(SolanaGetReceiptRequest request)
    {
        var node = request.Network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new($"Primary node is not configured on {request.Network.Name} network");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var epochInfoResponse = await rpcClient.GetEpochInfoAsync();

        TransactionResponse transaction;

        try
        {
            transaction = await GetTransactionReceiptAsync(
                rpcClient,
                request.Network,
                epochInfoResponse.Result,
                request.TxHash);
        }
        catch (AggregateException ae)
        {
            var transactionNotConfirmedException =
                ae.InnerExceptions.FirstOrDefault(c => c is TransactionNotComfirmedException);
            if (transactionNotConfirmedException is not null)
            {
                throw transactionNotConfirmedException;
            }

            var status = await rpcClient.GetSignatureStatusAsync(request.TxHash);

            if (status.Result.Value.FirstOrDefault() != null)
            {
                throw new TransactionNotComfirmedException($"Transaction is not confirmed yet, TxHash: {request.TxHash}.");
            }

            throw;
        }

        if (transaction.Status == TransactionStatus.Failed)
        {
            throw new TransactionFailedException("Transaction failed");
        }

        await CheckBlockHeightAsync(rpcClient, request.TransactionBlockHeight);

        return transaction;
    }

    [Activity]
    public virtual async Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request)
    {
        var node = request.Network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {request.Network.Name} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var blockProcessingTasks = new Dictionary<int, Task<HTLCBlockEventResponse>>();
        var blocksForProcessing = Enumerable.Range((int)request.FromBlock, (int)(request.ToBlock - request.FromBlock) + 1).ToArray();
        var events = new HTLCBlockEventResponse();

        foreach (var blockChunk in blocksForProcessing.Chunk(MaxConcurrentTaskCount))
        {
            foreach (var currentBlock in blockChunk)
            {
                blockProcessingTasks[currentBlock] = EventDecoder.GetBlockEventsAsync(
                    rpcClient,
                    request.Network,
                    request.WalletAddresses,
                    currentBlock);
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

    [Activity]
    public virtual async Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request)
    {
        var node = request.Network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {request.Network.Name} is not configured");
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

    [Activity]
    public Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request)
    {
        var currency = request.Network.Tokens.Single(x => x.Symbol.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency), $"Currency {request.Asset} for {request.Network.Name} is missing");
        }

        if (request.Signature is null)
        {
            throw new ArgumentNullException(nameof(request.Signature), "Signature is required");
        }

        var message = Ed25519Program.CreateAddLockSigMessage(new()
        {
            Hashlock = request.Hashlock.HexToByteArray(),
            Id = request.CommitId.HexToByteArray(),
            Timelock = request.Timelock,
            SignerPublicKey = new PublicKey(request.SignerAddress),
        });

        var signatureBytes = Convert.FromBase64String(request.Signature);
        var signerPublicKey = new PublicKey(request.SignerAddress).KeyBytes;

        var verifier = new Ed25519Signer();
        verifier.Init(false, new Ed25519PublicKeyParameters(signerPublicKey, 0));
        verifier.BlockUpdate(message, 0, message.Length);
        var isValid = verifier.VerifySignature(signatureBytes);

        return Task.FromResult(isValid);
    }

    [Activity]
    public async Task SimulateTransactionAsync(SolanaPublishTransactionRequest request)
    {
        var node = request.Network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {request.Network.Name} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var simulatedTransaction = await rpcClient.SimulateTransactionAsync(request.RawTx);

        if (!simulatedTransaction.WasSuccessful || simulatedTransaction.Result.Value.Error != null)
        {
            if (!simulatedTransaction.WasSuccessful)
            {
                throw new Exception($"Failed to simulate transaction in network {request.Network.Name}: Reason {simulatedTransaction.Reason}");
            }

            if (simulatedTransaction.Result.Value.Error.Type == TransactionErrorType.BlockhashNotFound)
            {
                throw new NonceMissMatchException(
                    $"Nonce mismatch error Failed to simulate transaction in network {request.Network.Name}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
            }

            throw new Exception($"Failed to simulate transaction in network {request.Network.Name}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
        }
    }

    [Activity]
    public async Task<SolanaComposeTransactionResponse> ComposeSolanaTranscationAsync(SolanaComposeTransactionRequest request)
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
        var tx = Transaction.Deserialize(transactionBytes);
        foreach (var instruction in tx.Instructions)
        {
            builder.AddInstruction(instruction);
        }

        if (tx.FeePayer != solanaAddress)
        {
            signers.Add(tx.FeePayer);
        }

        if (!SolanaConstants.MediumComputeUnitPrice.TryGetValue(request.Network.Name, out var computeUnitPrice))
        {
            throw new($"Compute unit is not configured for netwokr {request.Network.Name}");
        }

        var node = request.Network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node for network: {request.Network.Name} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder
            .AddInstruction(ComputeBudgetProgram.SetComputeUnitPrice((ulong)Web3.Convert.ToWei(computeUnitPrice, SolanaConstants.MicroLamportsDecimal)))
            .SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var rawTxResult = Convert.ToBase64String(builder.Serialize());

        return new()
        {
            LastValidBlockHeight = latestBlockHashResponse.Result.Value.LastValidBlockHeight.ToString(),
            RawTx = rawTxResult
        };
    }

    [Activity]
    public async Task<string> PublishTransactionAsync(SolanaPublishTransactionRequest request)
    {
        var node = request.Network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new($"Node for network: {request.Network.Name} is not configured");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var signedRawData = Convert.FromBase64String(request.RawTx);

        try
        {
            var transactionResult = await rpcClient.SendSolanaTransactionAsync(signedRawData);

            if (!transactionResult.WasSuccessful)
            {
                if (transactionResult.ServerErrorCode == BlockhashNotFoundErrorCode)
                {
                    throw new NonceMissMatchException($"Nonce miss match in network {request.Network.Name}, Reason: {transactionResult.RawRpcResponse}.");
                }

                throw new Exception($"Failed to submit {request.Network.Name} transaction due to error: {transactionResult.RawRpcResponse}");
            }

            if (!string.IsNullOrEmpty(transactionResult.Result))
            {
                return transactionResult.Result;
            }
        }
        catch (Exception ex)
        {
        }

        return CalculateTransactionHash(signedRawData);
    }

    private static bool ValidateAddress(string address)
        => PublicKey.IsValid(address);

    //private static BigInteger ComputeRentFee(
    //   string networkName,
    //   List<TransactionInstruction> instructions)
    //{
    //    int accountCreationCount = 0;

    //    foreach (var instruction in instructions)
    //    {
    //        var lockDescriminator = FieldEncoder.Sighash(SolanaConstants.LockSighash);

    //        if (instruction.Data.Take(8).SequenceEqual(lockDescriminator))
    //        {
    //            accountCreationCount++;
    //        }
    //    }

    //    return accountCreationCount * LamportsPerRent;
    //}

    private static string CalculateTransactionHash(byte[] rawTransactionBytes)
    {
        var tx = Solnet.Rpc.Models.Transaction.Deserialize(rawTransactionBytes);

        var firstSignature = tx.Signatures.First().Signature;

        var transactionHash = Encoders.Base58.EncodeData(firstSignature);

        return transactionHash;
    }

    private async Task CheckBlockHeightAsync(
        IRpcClient rpcClient,
        string lastValidBlockHeight)
    {
        var epochInfoResponseResult = await rpcClient.GetEpochInfoAsync();

        if (!epochInfoResponseResult.WasSuccessful)
        {
            throw new Exception($"Failed to get latestBlock");
        }

        if (ulong.Parse(lastValidBlockHeight) <= epochInfoResponseResult.Result.BlockHeight)
        {
            throw new TransactionFailedRetriableException("Transaction not found");
        }
    }

    private static async Task<TransactionResponse> GetTransactionReceiptAsync(
        IRpcClient rpcClient,
        DetailedNetworkDto network,
        EpochInfo epochInfo,
        string transactionId)
    {
        var feeCurrency = network.NativeToken;

        if (feeCurrency == null)
        {
            throw new($"Fee currency not cinfigured for network {network.Name}");
        }

        var transactionReceiptResult = await rpcClient.GetParsedTransactionAsync(transactionId);

        var confirmations = (int)(epochInfo.AbsoluteSlot - (ulong)transactionReceiptResult.Result.Slot) + 1;

        if (confirmations <= 0)
        {
            throw new TransactionNotComfirmedException($"Confirmations for transaction {transactionId} in network: {network.Name} is less then 0");
        }

        var result = new TransactionResponse
        {
            TransactionHash = transactionId,
            FeeAmount = transactionReceiptResult.Result.Meta.Fee,
            FeeAsset = feeCurrency.Symbol,
            FeeDecimals = feeCurrency.Decimals,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(transactionReceiptResult.Result.BlockTime * 1000),
            Status = transactionReceiptResult.Result.Meta.Err is null ? TransactionStatus.Completed : TransactionStatus.Failed,
            Confirmations = confirmations,
            NetworkName = network.Name,
        };

        return result;
    }

    public async Task<string> SignTransactionAsync(SolanaSignTransactionRequest request)
    {
        var signedTransaction = await privateKeyProvider.SignAsync(
            request.SignerAgentUrl,
            request.Network.Type,
            request.FromAddress,
            request.UnsignRawTransaction);

        if (string.IsNullOrEmpty(signedTransaction))
        {
            throw new Exception($"Failed to sign transaction for {request.FromAddress} on network {request.Network.Name}. RawTx {request.UnsignRawTransaction}");
        }

        return signedTransaction;
    }
}
