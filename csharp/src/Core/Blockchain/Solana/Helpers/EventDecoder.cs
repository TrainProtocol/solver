using FluentResults;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Solnet.Rpc.Models;
using Solnet.Rpc;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Errors;
using Train.Solver.Data.Entities;
using Train.Solver.Core.Blockchain.Solana.Extensions;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using System.Numerics;
using Solnet.Programs.Utilities;

namespace Train.Solver.Core.Blockchain.Solana.Helpers;

public static class EventDecoder
{

    public static async Task<Result<HTLCBlockEvent>> GetBlockEventsAsync(
        IRpcClient rpcClient,
        int block,
        Network network,
        List<Token> currencies)
    {
        var solverAccount = network.ManagedAccounts
          .FirstOrDefault(x => x.Type == AccountType.LP);

        if (solverAccount is null)
        {
            return Result.Fail(new NotFoundError($"Solver address for network: {network.Name} is not configured"));
        }

        var blockResponseResult = await rpcClient.GetParsedEventBlockAsync(block);

        if (blockResponseResult.IsFailed)
        {
            var errorMessage = blockResponseResult.Errors.First().Message;
            Serilog.Log.Error($"Failed to get block transactions for network {network.Name}: Reason {errorMessage}: Block {block}");

            return Result.Fail($"Fail to get block {block}");
        }

        var result = new HTLCBlockEvent();

        if (blockResponseResult.Value.Result != null && blockResponseResult.Value.Result.Transactions.Any())
        {
            var htlcTokenContractAddress = network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

            var trackedBlockEvents = blockResponseResult.Value.Result.Transactions
                .Where(transaction => transaction.Transaction.Message.Instructions
                .Any(instruction => instruction.ProgramId == htlcTokenContractAddress))
                .ToList();

            foreach (var transaction in trackedBlockEvents)
            {
                var isCommitEvent = transaction.Meta.LogMessages
                    .Any(x => x.Contains(SolanaConstants.HtlcConstants.commitEventPrefixPattern));

                var isLockEvent = transaction.Meta.LogMessages
                    .Any(x => x.Contains(SolanaConstants.HtlcConstants.addLockEventPrefixPattern));

                if (isCommitEvent)
                {
                    var prefixPattern = "Program return: " + htlcTokenContractAddress + " ";

                    var logResult = transaction.Meta.LogMessages.Where(s => s.StartsWith(prefixPattern))
                        .Select(s => s.Substring(prefixPattern.Length))
                        .First();

                    // find id from programs returned log 
                    var id = Convert.FromBase64String(logResult).ToHex(prefix: true);

                    var commitEventResult = await DeserializeCommitEventDataAsync(
                        rpcClient,
                        id,
                        network);

                    if (commitEventResult.IsFailed)
                    {
                        Serilog.Log.Error(commitEventResult.Errors.First().Message);
                        continue;
                    }

                    if (commitEventResult.Value.ReceiverAddress != solverAccount.Address)
                    {
                        continue;
                    }

                    var destinationCurrency = currencies
                        .FirstOrDefault(x => x.Asset == commitEventResult.Value.DestinationAsset
                        && x.Network.Name == commitEventResult.Value.DestinationNetwork);

                    if (destinationCurrency is null)
                    {
                        continue;
                    }

                    var sourceCurrency = network.Tokens
                        .FirstOrDefault(x => x.Asset == commitEventResult.Value.SourceAsset);

                    if (sourceCurrency is null)
                    {
                        continue;
                    }

                    var commitEventMessage = new HTLCCommitEventMessage
                    {
                        TxId = transaction.Transaction.Signatures.First(),
                        Id = commitEventResult.Value.Id,
                        Amount = Web3.Convert.FromWei(BigInteger.Parse(commitEventResult.Value.AmountInWei), sourceCurrency.Decimals),
                        AmountInWei = commitEventResult.Value.AmountInWei,
                        ReceiverAddress = solverAccount.Address,
                        SourceNetwork = network.Name,
                        SourceAsset = commitEventResult.Value.SourceAsset,
                        DestinationAddress = commitEventResult.Value.DestinationAddress,
                        DestinationNetwork = commitEventResult.Value.DestinationNetwork,
                        DestinationAsset = commitEventResult.Value.DestinationAsset,
                        SenderAddress = commitEventResult.Value.SenderAddress,
                        TimeLock = commitEventResult.Value.TimeLock
                    };

                    result.HTLCCommitEventMessages.Add(commitEventMessage);
                }

                if (isLockEvent)
                {
                    var prefixPattern = "Program return: " + htlcTokenContractAddress + " ";

                    var logResult = transaction.Meta.LogMessages.Where(s => s.StartsWith(prefixPattern))
                    .Select(s => s.Substring(prefixPattern.Length))
                        .First();

                    // find id from programs returned log 
                    var id = Convert.FromBase64String(logResult).ToHex(prefix: true);

                    var addLockMessageResult = await DeserializeAddLockEventDataAsync(
                        rpcClient,
                        id,
                        network);

                    if (addLockMessageResult.IsFailed)
                    {
                        Serilog.Log.Error(addLockMessageResult.Errors.First().Message);
                        continue;
                    }

                    addLockMessageResult.Value.TxId = transaction.Transaction.Signatures.First();

                    result.HTLCLockEventMessages.Add(addLockMessageResult.Value);
                }
            }
        }

        return Result.Ok(result);
    }

    private static async Task<Result<HTLCCommitEventMessage>> DeserializeCommitEventDataAsync(
       IRpcClient rpcClient,
       string commitId,
       Network network)
    {
        var solverAccount = network.ManagedAccounts
            .FirstOrDefault(x => x.Type == AccountType.LP);

        if (solverAccount is null)
        {
            return Result.Fail($"Solver address for network: {network.Name} is not configured");
        }


        if (!SolanaConstants.GetDetailsDescriminator.TryGetValue(network.Name, out var getDetailsDescriminator))
        {
            return Result.Fail($"Lock Descriminator nor configured for network: {network.Name} is not configured");
        }

        var builder = new TransactionBuilder()
           .SetFeePayer(new PublicKey(solverAccount!.Address));

        builder = SetDetailsInstruction(
            builder,
            network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address,
            getDetailsDescriminator,
            commitId.HexToByteArray());

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            return Result.Fail($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var rawTx = builder.Build(new Account());

        var simulatedTransaction = await rpcClient.SimulateTransactionAsync(rawTx);

        if (!simulatedTransaction.WasSuccessful || simulatedTransaction.Result.Value.Error != null)
        {
            if (!simulatedTransaction.WasSuccessful)
            {
                return Result.Fail($"Failed to simulate transaction in network {network.Name}: Reason {simulatedTransaction.Reason}");
            }

            return Result.Fail($"Failed to simulate transaction in network {network.Name}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
        }
        var response = GetHTLCCommitEventMessage(simulatedTransaction.Result.Value.Logs.ToList());

        response.Id = commitId;

        return Result.Ok(response);
    }

    private static async Task<Result<HTLCLockEventMessage>> DeserializeAddLockEventDataAsync(
       IRpcClient rpcClient,
       string id,
       Network network)
    {
        var solverAccount = network.ManagedAccounts
            .FirstOrDefault(x => x.Type == AccountType.LP);

        if (solverAccount is null)
        {
            return Result.Fail($"Solver address for network: {network.Name} is not configured");
        }

        if (!SolanaConstants.GetDetailsDescriminator.TryGetValue(network.Name, out var getDetailsDescriminator))
        {
            return Result.Fail($"Lock Descriminator nor configured for network: {network.Name} is not configured");
        }

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(solverAccount!.Address));

        builder = SetDetailsInstruction(
            builder,
            network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address,
            getDetailsDescriminator,
            id.HexToByteArray());

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            return Result.Fail($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var rawTx = builder.Build(new Account());

        var simulatedTransaction = await rpcClient.SimulateTransactionAsync(rawTx);

        if (!simulatedTransaction.WasSuccessful || simulatedTransaction.Result.Value.Error != null)
        {
            if (!simulatedTransaction.WasSuccessful)
            {
                return Result.Fail($"Failed to simulate transaction in network {network.Name}: Reason {simulatedTransaction.Reason}");
            }

            return Result.Fail($"Failed to simulate transaction in network {network.Name}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
        }
        var response = new HTLCLockEventMessage();

        response.Id = id;

        response.HashLock = ExtractString(
            simulatedTransaction.Result.Value.Logs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.hashlockLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.hashlockLogPrefixPattern).EnsureHexPrefix();

        response.TimeLock = long.Parse(ExtractValue(
           simulatedTransaction.Result.Value.Logs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.timelockLogPrefixPattern))!,
           SolanaConstants.HtlcConstants.timelockLogPrefixPattern));

        return response;
    }

    private static TransactionBuilder SetDetailsInstruction(
       TransactionBuilder builder,
       string htlcProgramIdKey,
       byte[] getDetailsDescriminator,
       byte[] id)
    {
        var htlc = PublicKey.TryFindProgramAddress(
            new List<byte[]>()
                {
                id,
                },
            new PublicKey(htlcProgramIdKey),
            out PublicKey htlcPubKey,
            out byte htlcBump);

        var fields = new List<FieldEncoder.Field>
    {
        new FieldEncoder.Field
        {
            Span = id.Length,
            Property = "id",
            EncoderFunc = (value, buffer, offset) =>
            {
                var byteArray = (byte[]) value;
                FieldEncoder.EncodeByteArray(byteArray, buffer, ref offset);
            }
        },
        new FieldEncoder.Field
        {
            Span = 1,
            Property = "bump",
            EncoderFunc = (value, buffer, offset) =>
            {
                var byteValue = (byte)value;
                buffer.WriteU8(byteValue, offset);
            }
        }
    };

        var src = new Dictionary<string, object>
    {
        { "id", id },
        { "bump", htlcBump }
    };

        var keys = new List<AccountMeta>
    {
          AccountMeta.ReadOnly(publicKey: htlcPubKey,isSigner: false)
    };

        builder.AddInstruction(new()
        {
            ProgramId = new PublicKey(htlcProgramIdKey).KeyBytes,
            Keys = keys,
            Data = FieldEncoder.Encode(fields, src, getDetailsDescriminator)
        });

        return builder;
    }

    private static HTLCCommitEventMessage GetHTLCCommitEventMessage(List<string> commitLogs)
    {
        var response = new HTLCCommitEventMessage();

        response.DestinationAddress = ExtractString(
            commitLogs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.destinationAddressLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.destinationAddressLogPrefixPattern);

        response.DestinationAsset = ExtractString(
            commitLogs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.destinationAssetLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.destinationAssetLogPrefixPattern);

        response.DestinationNetwork = ExtractString(
            commitLogs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.destinationNetworkLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.destinationNetworkLogPrefixPattern);

        response.SourceAsset = ExtractString(
            commitLogs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.sourceAssetLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.sourceAssetLogPrefixPattern);

        response.AmountInWei = ExtractValue(
            commitLogs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.amountInWeiLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.amountInWeiLogPrefixPattern);

        response.TimeLock = long.Parse(ExtractValue(
            commitLogs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.timelockLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.timelockLogPrefixPattern));

        response.ReceiverAddress = ExtractValue(
            commitLogs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.receiverLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.receiverLogPrefixPattern);

        response.SenderAddress = ExtractValue(
            commitLogs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.senderLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.senderLogPrefixPattern);

        return response;
    }


    private static string ExtractString(string eventLog, string prefixPattern)
    {
        var startMarker = "Program log: " + prefixPattern + ": \"";

        var startIndex = eventLog.IndexOf(startMarker) + startMarker.Length;

        var endIndex = eventLog.IndexOf("\"", startIndex);

        return eventLog.Substring(startIndex, endIndex - startIndex);
    }

    private static string ExtractValue(string eventLog, string prefixPattern)
    {
        var startMarker = "Program log: " + prefixPattern;

        return eventLog.Substring(startMarker.Length);
    }
}
