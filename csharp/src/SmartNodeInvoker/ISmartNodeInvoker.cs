using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Common.Enums;

namespace Train.Solver.SmartNodeInvoker;

public interface ISmartNodeInvoker
{
    Task<NodeResult<T>> ExecuteAsync<T>(
        string networkName,
        IEnumerable<string> nodes,
        Func<string, Task<T>> dataRetrievalTask);
}
