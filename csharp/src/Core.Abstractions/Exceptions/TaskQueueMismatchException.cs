namespace Train.Solver.Infrastructure.Abstractions.Exceptions;

public class TaskQueueMismatchException : Exception
{
    public TaskQueueMismatchException()
        : base("Activity's implementation is not registered in current task queue")
    {
    }
}
