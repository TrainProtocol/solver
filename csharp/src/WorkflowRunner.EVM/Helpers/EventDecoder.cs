using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Train.Solver.WorkflowRunner.EVM.Models;

namespace Train.Solver.WorkflowRunner.EVM.Helpers;

public static class EventDecoder
{
    private static readonly Type[] KnownEventTypes =
    [
        typeof(EtherTokenCommittedEvent),
        typeof(ERC20TokenCommitedEvent),
        typeof(EtherTokenLockAddedEvent),
    ];

    public static (Type eventType, object decodedEvent)? Decode(FilterLog log)
    {
        foreach (var dtoType in KnownEventTypes)
        {
            var eventGenericType = typeof(Event<>).MakeGenericType(dtoType);
            var decodeMethod = eventGenericType.GetMethod("DecodeEvent", [typeof(FilterLog)]);

            if (decodeMethod != null)
            {
                var decoded = decodeMethod.Invoke(null, [log]);
                if (decoded != null)
                {
                    var eventProp = decoded.GetType().GetProperty("Event");
                    var typedEvent = eventProp?.GetValue(decoded);

                    if (typedEvent != null)
                        return (dtoType, typedEvent);
                }
            }
        }

        return null;
    }
}

