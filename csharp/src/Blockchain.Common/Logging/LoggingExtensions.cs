using Microsoft.Extensions.Logging;

namespace Train.Solver.Blockchain.Common;

/// <summary>
///     Logs a structured message using the compile-time source generator for high-performance logging.
/// </summary>
/// <remarks>
/// VS might show build error, since source generator works compile-time.
/// See: https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator
/// </remarks>
public static partial class TemporalLoggingExtensions
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Event listener: `{eventListenerId}` was not running. Started.")]
    public static partial void EventListeningStarted(
        this ILogger logger, 
        string eventListenerId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Event listener: `{eventListenerId}` should be terminated. Terminated.")]
    public static partial void EventListeningTerminated(
        this ILogger logger,
        string eventListenerId);
}
