using Nethereum.Hex.HexConvertors.Extensions;
using Train.Solver.Core.Blockchains.Starknet.Models;
using Train.Solver.Core.Extensions;
using static Train.Solver.Core.Blockchains.Starknet.Models.GetEventsResponse;

namespace Train.Solver.Core.Blockchains.Starknet.Extensions;
public static class EventDataExtensions
{
    public static StarknetTokenLockedEvent DeserializeLockEventData(this EventData eventData)
    {
        // Every Uint256 in StarknetTransactionProccessorWorkflow consinsts from 2 hex strings
        if (!BigIntegerExtensions.TryParse(eventData.Keys[1], eventData.Keys[2], out var id))
        {
            throw new Exception($"Failed to deserialize commitId in lock event: txHash {eventData.TransactionHash}");
        }

        var cursor = 0;

        if (!BigIntegerExtensions.TryParse(eventData.Data[cursor++], eventData.Data[cursor++], out var hashlock))
        {
            throw new Exception($"Failed to deserialize timelock in lock event: txHash {{eventData.TransactionHash}}");
        }

        if (!BigIntegerExtensions.TryParse(eventData.Data[cursor++], eventData.Data[cursor++], out var timelock))
        {
            throw new Exception($"Failed to deserialize timelock in lock event: txHash {{eventData.TransactionHash}}");
        }

        return new StarknetTokenLockedEvent()
        {
            Hashlock = hashlock,
            Timelock = timelock,
            Id = id
        };
    }

    public static StarknetTokenCommittedEvent DeserializeCommitEventData(this EventData eventData)
    {
        var cursor = 0;

        if (!BigIntegerExtensions.TryParse(eventData.Keys[1], eventData.Keys[2], out var id))
        {
            throw new Exception($"Failed to deserialize commitId in commit event: txHash {eventData.TransactionHash}");
        }
        var senderAddress = eventData.Keys[3];
        var receiverAddress = eventData.Keys[4];

        var hopChainsCount = Convert.ToInt32(eventData.Data[cursor], 16);
        cursor++;

        var hopChains = new List<string>();

        for (int i = 0; i < hopChainsCount; i++)
        {
            hopChains.Add(eventData.Data[cursor + i].HexToUTF8String());
            cursor++;
        }

        var hopAssetsCount = Convert.ToInt32(eventData.Data[cursor], 16);
        cursor++;

        var hopAssets = new List<string>();

        for (int i = 0; i < hopAssetsCount; i++)
        {
            hopAssets.Add(eventData.Data[cursor + i].HexToUTF8String());
            cursor++;
        }

        var hopAddressCount = Convert.ToInt32(eventData.Data[cursor], 16);
        cursor++;

        var hopAddresses = new List<string>();

        for (int i = 0; i < hopAddressCount; i++)
        {
            hopAddresses.Add(eventData.Data[cursor + i]);
            cursor++;
        }

        var destinationNetwork = eventData.Data[cursor++].HexToUTF8String();
        var destatinationAddressArrayLength = Convert.ToInt32(eventData.Data[cursor++], 16);
        var hexadecimalAddress = string.Empty;

        for (int i = 0; i <= destatinationAddressArrayLength; i++)
        {
            hexadecimalAddress += eventData.Data[cursor++].RemoveHexPrefix();
        }
        var destinationAddress = hexadecimalAddress.HexStringToAscii();

        // starknet returnes in cursor position byte array last pending words symbol count
        cursor++;

        var destinationAsset = eventData.Data[cursor++].HexToUTF8String();
        var sourceAsset = eventData.Data[cursor++].HexToUTF8String();

        if (!BigIntegerExtensions.TryParse(eventData.Data[cursor], eventData.Data[cursor + 1], out var amountInBaseUnits))
        {
            throw new Exception($"Failed to deserialize amount in commit event: txHash {eventData.TransactionHash}");
        }
        cursor += 2;

        if (!BigIntegerExtensions.TryParse(eventData.Data[cursor], eventData.Data[cursor + 1], out var timelock))
        {
            throw new Exception($"Failed to deserialize timelock in commit event: txHash {eventData.TransactionHash}");
        }
        cursor += 2;

        var tokenContract = eventData.Data[cursor++];

        return new StarknetTokenCommittedEvent()
        {
            Id = id,
            HopChains = hopChains,
            HopAddress = hopAddresses,
            HopAssets = hopAssets,
            DestinationNetwork = destinationNetwork,
            DestinationAddress = destinationAddress,
            DestinationAsset = destinationAsset,
            SourceAsset = sourceAsset,
            AmountInBaseUnits = amountInBaseUnits.ToString(),
            Timelock = timelock,
            SenderAddress = senderAddress,
            TokenContract = tokenContract,
            SourceReciever = receiverAddress
        };
    }
}
