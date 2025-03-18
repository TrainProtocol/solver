using FluentResults;
using Microsoft.EntityFrameworkCore;
using Nethereum.Web3;
using Serilog;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;
using Temporalio.Activities;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Blockchain.Solana;
using Train.Solver.Core.Blockchain.Solana.Extensions;
using Train.Solver.Core.Blockchain.Solana.Helpers;
using Train.Solver.Core.Blockchain.Solana.Programs;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Secret;
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using Train.Solver.WorkflowRunner.Exceptions;
using TransactionModel = Train.Solver.Core.Temporal.Abstractions.Models.TransactionModel;

namespace Train.Solver.WorkflowRunner.TransactionProcessor.Solana;

public class SolanaActivities(
    IPrivateKeyProvider privateKeyProvider,
    SolverDbContext dbContext,
    IServiceProvider serviceProvider)
{
    private const int BlockhashNotFoundErrorCode = -32002;

    [Activity]
    public async Task IsValidSolanaTransactionAsync(
        string networkName,
        byte[] rawTx)
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

        var simulatedTransaction = await rpcClient.SimulateTransactionAsync(rawTx);

        if (!simulatedTransaction.WasSuccessful || simulatedTransaction.Result.Value.Error != null)
        {
            if (!simulatedTransaction.WasSuccessful)
            {
                throw new($"Failed to simulate transaction in network {network.Name}: Reason {simulatedTransaction.Reason}");
            }
            else if (simulatedTransaction.Result.Value.Error.Type == Solnet.Rpc.Models.TransactionErrorType.BlockhashNotFound)
            {
                throw new NonceMissMatchException($"Failed to simulate transaction in network {network.Name}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
            }

            throw new($"Failed to simulate transaction in network {network.Name}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
        }
    }

    [Activity]
    public async Task<string> SolanaSendTransactionAsync(
        string networkName,
        byte[] rawTx)
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

                throw new($"Failed to submit {network.Name} transaction due to error: {transactionResult.RawRpcResponse}");
            }

            if (!string.IsNullOrEmpty(transactionResult.Result))
            {
                return transactionResult.Result;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Occurred exception while sending raw transaction in network {network.Name}. Message {ex.Message}.");
        }

        return CalculateTransactionHash(rawTx);
    }

    [Activity]
    public async Task<TransactionModel> GetSolanaTransactionReceiptAsync(
        string networkName,
        string fromAddress,
        string transactionHash)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            throw new("Invalid network");
        }
        var blockchainService = serviceProvider.GetKeyedService<ISolanaBlockchainService>(network.Group);

        if (blockchainService is null)
        {
            throw new($"Transaction builder is not registered for {network.Group}");
        }

        var transactionReceiptResult = await blockchainService.GetConfirmedTransactionAsync(
            networkName,
            transactionHash);

        if (transactionReceiptResult.IsFailed)
        {
            var checkBlockHeightResult = await blockchainService.CheckBlockHeightAsync(network, fromAddress);

            if (checkBlockHeightResult.HasError<TransactionFailedRetryableError>())
            {
                throw new TransactionFailedRetriableException($"Transaction not found for transaction hash {transactionHash}, BlockHeight is not valid.");
            }
            else if (transactionReceiptResult.HasError<TransactionNotConfirmedError>())
            {
                throw new($"Transaction with Id {transactionHash} is not confirmed yet. Network {networkName}.");
            }
            else
            {
                var message = transactionReceiptResult.Errors.First().Message;

                throw new($"Failed to get receipt in network {network}, Reason {message}.");
            }
        }

        var transaction = new TransactionModel
        {
            NetworkName = networkName,
            Status = TransactionStatus.Completed,
            TransactionHash = transactionReceiptResult.Value.TransactionId,
            FeeAmount = transactionReceiptResult.Value.FeeAmount,
            FeeAsset = transactionReceiptResult.Value.FeeAsset,
            Timestamp = transactionReceiptResult.Value.Timestamp != default
            ? DateTimeOffset.FromUnixTimeMilliseconds(transactionReceiptResult.Value.Timestamp)
                : DateTimeOffset.UtcNow,
            Confirmations = transactionReceiptResult.Value.Confirmations
        };

        return transaction;
    }

    [Activity]
    public async Task<byte[]> ComposeSolanaTranscationAsync(
        Fee fee,
        string fromAddress,
        string callData,
        string lastValidBLockHash)
    {
        var solanaAddress = new PublicKey(fromAddress);

        var builder = new TransactionBuilder()
            .SetFeePayer(solanaAddress);

        var signers = new List<string> { solanaAddress };

        if (string.IsNullOrEmpty(callData))
        {
            throw new("Call data is required");
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

        if (rawTxResult.IsFailed)
        {
            throw new($"Failed to sign transaction: {rawTxResult.ToResult()}");
        }

        return rawTxResult.Value;
    }
    

    private async Task<Result<byte[]>> SignSolanaTransactionAsync(
        TransactionBuilder builder,
        List<string> managedAddresses)
    {
        var signers = new List<Account>();

        foreach (var address in managedAddresses)
        {
            var privateKeyResult = await privateKeyProvider.GetAsync(address);

            if (privateKeyResult.IsFailed)
            {
                return Result.Fail($"Unable to get private key for address {address}.");
            }

            var solanaAccount = new Account(privateKeyResult.Value, address);
            signers.Add(solanaAccount);
        }

        return builder.Build(signers);
    }

    private static string CalculateTransactionHash(byte[] rawTransactionBytes)
    {
        var tx = Solnet.Rpc.Models.Transaction.Deserialize(rawTransactionBytes);

        var firstSignature = tx.Signatures.First().Signature;

        var transactionHash = Encoders.Base58.EncodeData(firstSignature);

        return transactionHash;
    }
}
