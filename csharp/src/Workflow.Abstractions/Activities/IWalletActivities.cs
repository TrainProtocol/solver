using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Infrastructure.Abstractions.Models;

namespace Train.Solver.Workflow.Abstractions.Activities;

public interface IWalletActivities
{
    Task<SignerAgentDto> GetSignerAgentAsync(string name);
}
