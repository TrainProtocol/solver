using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Solnet.Programs.Utilities;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Wallet;
using Train.Solver.Blockchains.Solana.Extensions;
using Train.Solver.Blockchains.Solana.Models;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Models.HTLCModels;

namespace Train.Solver.Blockchains.Solana.Helpers;

public static class EventDecoder
{
    public static async Task<HTLCBlockEventResponse> GetBlockEventsAsync(
        IRpcClient rpcClient,
        int block,
        Network network,
        List<Token> currencies)
    {
        var solverAccount = network.ManagedAccounts
            .Single(x => x.Type == AccountType.LP);

        var blockResponseResult = await rpcClient.GetParsedEventBlockAsync(block);

        if (blockResponseResult is null)
        {
            throw new Exception($"Failed to get block {block}");
        }

        var result = new HTLCBlockEventResponse();

        if (blockResponseResult.Result != null && blockResponseResult.Result.Transactions.Any())
        {
            var htlcTokenContractAddress = network.Contracts
                .First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

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
                        network);

                    if (commitEvent == null)
                    {
                        continue;
                    }

                    if (commitEvent.ReceiverAddress != solverAccount.Address)
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
                        Amount = Web3.Convert.FromWei(BigInteger.Parse(commitEvent.AmountInWei),
                            sourceCurrency.Decimals),
                        AmountInWei = commitEvent.AmountInWei,
                        ReceiverAddress = solverAccount.Address,
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
                        network);

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
        Network network)
    {
        var solverAccount = network.ManagedAccounts
            .FirstOrDefault(x => x.Type == AccountType.LP);

        if (solverAccount is null)
        {
            throw new ArgumentNullException(nameof(solverAccount),
                $"Solver address for network: {network.Name} is not configured");
        }

        if (!SolanaConstants.GetDetailsDescriminator.TryGetValue(network.Name, out var getDetailsDescriminator))
        {
            throw new ArgumentNullException("Get Details Descriminator",
                $"Lock Discriminator is not configured for network: {network.Name} is not configured");
        }

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(solverAccount!.Address));

        builder = SetDetailsInstruction(
            builder,
            network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address,
            getDetailsDescriminator,
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
        Network network)
    {
        try
        {
            var solverAccount = network.ManagedAccounts
                .FirstOrDefault(x => x.Type == AccountType.LP);

            if (solverAccount is null)
            {
                throw new ArgumentNullException(nameof(solverAccount),
                    $"Solver address for network: {network.Name} is not configured");
            }

            if (!SolanaConstants.GetDetailsDescriminator.TryGetValue(network.Name, out var getDetailsDescriminator))
            {
                throw new ArgumentNullException("Get Details Descriminator",
                    $"Lock Descriminator is not configured for network: {network.Name} is not configured");
            }

            var builder = new TransactionBuilder()
                .SetFeePayer(new PublicKey(solverAccount!.Address));

            builder = SetDetailsInstruction(
                builder,
                network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address,
                getDetailsDescriminator,
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
                    var byteArray = (byte[])value;
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
            AccountMeta.ReadOnly(publicKey: htlcPubKey, isSigner: false)
        };

        builder.AddInstruction(new()
        {
            ProgramId = new PublicKey(htlcProgramIdKey).KeyBytes,
            Keys = keys,
            Data = FieldEncoder.Encode(fields, src, getDetailsDescriminator)
        });

        return builder;
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
