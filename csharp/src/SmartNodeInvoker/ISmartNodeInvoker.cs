using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.SmartNodeInvoker;

public interface ISmartNodeInvoker
{
    Task<NodeResult<T>> ExecuteAsync<T>(
     IEnumerable<string> nodes,
     Func<string, Task<T>> dataRetrievalTask);
}
