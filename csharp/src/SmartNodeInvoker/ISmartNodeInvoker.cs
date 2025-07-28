using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.SmartNodeInvoker;

public interface ISmartNodeInvoker
{
    Task<NodeResult<T>> GetDataFromNodesAsync<T>(
     IEnumerable<string> nodes,
     Func<string, CancellationToken, Task<T>> dataRetrievalTask,
     CancellationToken cancellationToken = default);

    Task<NodeResult<T>> GetDataFromNodesParallelAsync<T>(
        IEnumerable<string> nodes,
        Func<string, CancellationToken, Task<T>> dataRetrievalTask,
        CancellationToken cancellationToken = default);
}
