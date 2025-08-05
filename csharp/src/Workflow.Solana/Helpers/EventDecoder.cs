using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Blockchain.Solana.Extensions;
using Train.Solver.Blockchain.Solana.Models;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram;
using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Solana.Helpers;

public static class EventDecoder
{
    public static async Task<HTLCBlockEventResponse> GetBlockEventsAsync(
        IRpcClient rpcClient,
        int block,
        Network network,        
        List<Token> currencies,
        string solverAccount)
    {
        var blockResponseResult = await rpcClient.GetParsedEventBlockAsync(block);

        if (blockResponseResult is null)
        {
            throw new Exception($"Failed to get block {block}");
        }

        var result = new HTLCBlockEventResponse();

        if (blockResponseResult.Result != null && blockResponseResult.Result.Transactions.Any())
        {
            var htlcTokenContractAddress = network.HTLCTokenContractAddress;

            var trackedBlockEvents = blockResponseResult.Result.Transactions
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

                    var commitEvent = await DeserializeCommitEventDataAsync(
                        rpcClient,
                        id,
                        network,
                        solverAccount);

                    if (commitEvent == null)
                    {
                        continue;
                    }

                    if (commitEvent.ReceiverAddress != solverAccount)
                    {
                        continue;
                    }

                    var destinationCurrency = currencies
                        .FirstOrDefault(x => x.Asset == commitEvent.DestinationAsset
                                             && x.Network.Name == commitEvent.DestinationNetwork);

                    if (destinationCurrency is null)
                    {
                        continue;
                    }

                    var sourceCurrency = network.Tokens
                        .FirstOrDefault(x => x.Asset == commitEvent.SourceAsset);

                    if (sourceCurrency is null)
                    {
                        continue;
                    }

                    var commitEventMessage = new HTLCCommitEventMessage
                    {
                        TxId = transaction.Transaction.Signatures.First(),
                        Id = commitEvent.Id,
                        AmountInWei = commitEvent.AmountInWei,
                        ReceiverAddress = solverAccount,
                        SourceNetwork = network.Name,
                        SourceAsset = commitEvent.SourceAsset,
                        DestinationAddress = commitEvent.DestinationAddress,
                        DestinationNetwork = commitEvent.DestinationNetwork,
                        DestinationAsset = commitEvent.DestinationAsset,
                        SenderAddress = commitEvent.SenderAddress,
                        TimeLock = commitEvent.TimeLock,
                        DestinationNetworkType = destinationCurrency.Network.Type,
                        SourceNetworkType = sourceCurrency.Network.Type
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
                        network,
                        solverAccount);

                    if (addLockMessageResult == null)
                    {
                        continue;
                    }

                    addLockMessageResult.TxId = transaction.Transaction.Signatures.First();

                    result.HTLCLockEventMessages.Add(addLockMessageResult);
                }
            }
        }

        return result;
    }

    private static async Task<SolanaHTLCCommitEventModel> DeserializeCommitEventDataAsync(
        IRpcClient rpcClient,
        string commitId,
        Network network,
        string solverAccount)
    {
        if (solverAccount is null)
        {
            throw new ArgumentNullException(nameof(solverAccount),
                $"Solver address for network: {network.Name} is not configured");
        }

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(solverAccount));

        builder.SetGetDetailsInstruction(
            new PublicKey(network.HTLCTokenContractAddress),
            commitId.HexToByteArray());

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            throw new Exception(
                $"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var rawTx = builder.Build(new Account());

        var simulatedTransaction = await rpcClient.SimulateTransactionAsync(rawTx);

        if (!simulatedTransaction.WasSuccessful || simulatedTransaction.Result.Value.Error != null)
        {
            if (!simulatedTransaction.WasSuccessful)
            {
                throw new Exception(
                    $"Failed to simulate transaction in network {network.Name}: Reason {simulatedTransaction.Reason}");
            }

            throw new Exception(
                $"Failed to simulate transaction in network {network.Name}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
        }

        var response = GetHTLCCommitEventMessage(simulatedTransaction.Result.Value.Logs.ToList());

        response.Id = commitId;

        return response;

    }

    private static async Task<HTLCLockEventMessage?> DeserializeAddLockEventDataAsync(
        IRpcClient rpcClient,
        string id,
        Network network,
        string solverAccount)
    {
        try
        {
            if (solverAccount is null)
            {
                throw new ArgumentNullException(nameof(solverAccount),
                    $"Solver address for network: {network.Name} is not configured");
            }

            var builder = new TransactionBuilder()
                .SetFeePayer(new PublicKey(solverAccount));

            var htlcContractAddress = network.HTLCTokenContractAddress;

            builder.SetGetDetailsInstruction(
                new PublicKey(htlcContractAddress),
                id.HexToByteArray());

            var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

            if (!latestBlockHashResponse.WasSuccessful)
            {
                throw new Exception(
                    $"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
            }

            builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

            var rawTx = builder.Build(new Account());

            var simulatedTransaction = await rpcClient.SimulateTransactionAsync(rawTx);

            if (!simulatedTransaction.WasSuccessful || simulatedTransaction.Result.Value.Error != null)
            {
                if (!simulatedTransaction.WasSuccessful)
                {
                    throw new Exception(
                        $"Failed to simulate transaction in network {network.Name}: Reason {simulatedTransaction.Reason}");
                }

                throw new Exception(
                    $"Failed to simulate transaction in network {network.Name}: Error Type {simulatedTransaction.Result.Value.Error.Type}");
            }

            var response = new HTLCLockEventMessage();

            response.Id = id;

            response.HashLock = ExtractString(
                simulatedTransaction.Result.Value.Logs.FirstOrDefault(x =>
                    x.Contains(SolanaConstants.HtlcConstants.hashlockLogPrefixPattern))!,
                SolanaConstants.HtlcConstants.hashlockLogPrefixPattern).EnsureHexPrefix();

            response.TimeLock = long.Parse(ExtractValue(
                simulatedTransaction.Result.Value.Logs.FirstOrDefault(x =>
                    x.Contains(SolanaConstants.HtlcConstants.timelockLogPrefixPattern))!,
                SolanaConstants.HtlcConstants.timelockLogPrefixPattern));

            return response;
        }

        catch (Exception e)
        {
            return null;
        }
    }

    private static SolanaHTLCCommitEventModel GetHTLCCommitEventMessage(List<string> commitLogs)
    {
        var response = new SolanaHTLCCommitEventModel();

        response.DestinationAddress = ExtractString(
            commitLogs.FirstOrDefault(x =>
                x.Contains(SolanaConstants.HtlcConstants.destinationAddressLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.destinationAddressLogPrefixPattern);

        response.DestinationAsset = ExtractString(
            commitLogs.FirstOrDefault(x => x.Contains(SolanaConstants.HtlcConstants.destinationAssetLogPrefixPattern))!,
            SolanaConstants.HtlcConstants.destinationAssetLogPrefixPattern);

        response.DestinationNetwork = ExtractString(
            commitLogs.FirstOrDefault(x =>
                x.Contains(SolanaConstants.HtlcConstants.destinationNetworkLogPrefixPattern))!,
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
