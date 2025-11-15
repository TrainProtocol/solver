namespace Train.Solver.SmartNodeInvoker;

public interface ISmartNodeInvoker
{
    Task<NodeResult<T>> ExecuteAsync<T>(
        string networkName,
        IEnumerable<string> nodes,
        Func<string, Task<T>> dataRetrievalTask);
}
