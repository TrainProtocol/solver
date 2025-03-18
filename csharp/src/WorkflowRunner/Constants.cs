using Temporalio.Workflows;
using Train.Solver.WorkflowRunner.Exceptions;

namespace Train.Solver.WorkflowRunner;

public static class Constants
{
    public const string CSharpTaskQueue = "atomic";
    public const string JsTaskQueue = "atomicJs";

    public static ActivityOptions DefaultJsActivityOptions = new()
    {
        ScheduleToCloseTimeout = TimeSpan.FromDays(2),
        StartToCloseTimeout = TimeSpan.FromHours(1),
        TaskQueue = JsTaskQueue
    };

    public static ActivityOptions DefaultActivityOptions = new()
    {
        ScheduleToCloseTimeout = TimeSpan.FromDays(2),
        StartToCloseTimeout = TimeSpan.FromHours(1),
    };

    public static ActivityOptions GetMarketPriceActivityOptions = new()
    {
        ScheduleToCloseTimeout = TimeSpan.FromDays(2),
        RetryPolicy = new()
        {
            InitialInterval = TimeSpan.FromSeconds(10),
            MaximumAttempts = 5,
            BackoffCoefficient = 1,
        }
    };
   
    public static ActivityOptions SolanaRetryableActivityOptions = new()
    {
        ScheduleToCloseTimeout = DefaultActivityOptions.ScheduleToCloseTimeout,
        StartToCloseTimeout = DefaultActivityOptions.StartToCloseTimeout,
        RetryPolicy = new()
        {
            NonRetryableErrorTypes = new[]
            {
                typeof(NonceMissMatchException).Name,
                typeof(TransactionFailedRetriableException).Name
            }
        }
    };
}
