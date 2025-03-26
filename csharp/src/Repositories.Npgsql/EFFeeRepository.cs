using Microsoft.EntityFrameworkCore;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Repositories;

namespace Train.Solver.Repositories.Npgsql;

public class EFFeeRepository(SolverDbContext dbContext) : IFeeRepository
{
    public async Task<List<Expense>> GetExpensesAsync()
    {
        return await dbContext.Expenses.ToListAsync();
    }

    public async Task<List<ServiceFee>> GetServiceFeesAsync()
    {
        return await dbContext.ServiceFees.ToListAsync();
    }

    public async Task UpdateExpenseAsync(string networkName, string tokenAsset, string feeAsset, decimal fee, TransactionType transactionType)
    {
        var feeToken = await dbContext.Tokens
            .Include(x => x.Network)
            .FirstOrDefaultAsync(x => x.Asset == feeAsset && x.Network.Name == networkName);

        if (feeToken == null)
        {
            throw new Exception($"Fee token {feeAsset} not found in network {networkName}");
        }

        var token = await dbContext.Tokens
            .Include(x => x.Network)
            .FirstOrDefaultAsync(x => x.Asset == tokenAsset && x.Network.Name == networkName);

        if (token == null)
        {
            throw new Exception($"Token {tokenAsset} not found in network {networkName}");
        }

        var expense = await dbContext.Expenses.FirstOrDefaultAsync(x =>
                   x.TokenId == token.Id && x.FeeTokenId == feeToken.Id && x.TransactionType == transactionType);

        if (expense == null)
        {
            expense = new Expense
            {
                TokenId = token.Id,
                FeeTokenId = feeToken.Id,
                TransactionType = transactionType
            };

            expense.AddFeeValue(fee);

            dbContext.Expenses.Add(expense);
        }

        expense.AddFeeValue(fee);

        await dbContext.SaveChangesAsync();
    }
}