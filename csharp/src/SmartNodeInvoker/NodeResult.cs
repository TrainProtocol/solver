using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.SmartNodeInvoker;

public class NodeResult<T>
{
    public bool Succeeded => SuccessfulNode != null;
    public T? Data { get; set; }
    public string? SuccessfulNode { get; set; }
    public Dictionary<string, Exception> FailedNodes { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
}