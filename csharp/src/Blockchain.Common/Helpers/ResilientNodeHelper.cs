using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Blockchain.Common.Helpers;

public static class ResilientNodeHelper
{
    public static async Task<T> GetDataFromNodesAsync<T>(IEnumerable<string> nodes, Func<string, Task<T>> dataRetrievalTask)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException(nameof(nodes), "Collection of nodes is null");
        }

        if (!nodes.Any())
        {
            throw new ArgumentException("Collection of nodes is empty", nameof(nodes));
        }

        var exceptions = new List<Exception>();

        foreach (var node in nodes)
        {
            try
            {
                var taskResult = await dataRetrievalTask(node);
                return taskResult;
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        if (exceptions.Count != 0)
        {
            throw new AggregateException("All nodes failed to respond",
                exceptions.DistinctBy(c => new { Type = c.GetType(), c.Message }));
        }

        throw new Exception("Failed to retrieve data from nodes");
    }

}