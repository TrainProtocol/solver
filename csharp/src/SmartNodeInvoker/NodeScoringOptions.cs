using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Train.Solver.SmartNodeInvoker;

public class NodeScoringOptions
{
    public int MaxScore { get; set; } = 100;
    public int MinScore { get; set; } = 0;
    public int CircuitBreakerThreshold { get; set; } = 20;
    public TimeSpan ScoreDecayInterval { get; set; } = TimeSpan.FromMinutes(10);
    public int DecayStep { get; set; } = 5;
    public Dictionary<string, int> FailurePenalties { get; set; } = new()
    {
        ["Timeout"] = 25,
        ["Http401"] = 15,
        ["Http429"] = 40,
        ["Http5xx"] = 25,
        ["Other"] = 10
    };
    public int SuccessReward { get; set; } = 5;
}