using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Infrastructure.Extensions;
using Train.Solver.Workflow.Abstractions.Activities;

namespace Train.Solver.Workflow.Swap.Activities;
public class WalletActivities(ISignerAgentRepository signerAgentRepository) : IWalletActivities
{
    public async Task<SignerAgentDto> GetSignerAgentAsync(string name)
    {
        var signerAgent = await signerAgentRepository.GetAsync(name);

        if (signerAgent == null)
        {
            throw new InvalidOperationException("Signer agent not found");
        }

        return signerAgent.ToDto();
    }
}
