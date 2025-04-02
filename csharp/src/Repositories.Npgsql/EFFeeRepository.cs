using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Train.Solver.Core.Abstractions.Entities;
using Train.Solver.Core.Abstractions.Models;
using Train.Solver.Core.Abstractions.Repositories;

namespace Train.Solver.Repositories.Npgsql;

public class EFFeeRepository(INetworkRepository networkRepository, SolverDbContext dbContext) : IFeeRepository
{
    public async Task<List<Expense>> GetExpensesAsync()
    {
        return await dbContext.Expenses.ToListAsync();
    }

    public async Task<List<ServiceFee>> GetServiceFeesAsync()
    {
        return await dbContext.ServiceFees.ToListAsync();
    }

    public async Task UpdateExpenseAsync(string networkName, string tokenAsset, string feeAsset, decimal feeAmount, TransactionType transactionType)
    {
        var feeToken = await networkRepository.GetTokenAsync(networkName, tokenAsset);

        if (feeToken == null)
        {
            throw new Exception($"Fee token {feeAsset} not found in network {networkName}");
        }

        var token = await networkRepository.GetTokenAsync(networkName, tokenAsset);

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

            dbContext.Expenses.Add(expense);
        }

        if (expense.LastFeeValues.Length == 0)
        {
            expense.LastFeeValues = expense.LastFeeValues.Append(feeAmount).ToArray();
        }
        else
        {
            if (feeAmount > expense.LastFeeValues.Average() * 30)
            {
                return;
            }

            expense.LastFeeValues = expense.LastFeeValues.Append(feeAmount).ToArray();

            if (expense.LastFeeValues.Length > 10)
            {
                expense.LastFeeValues = expense.LastFeeValues.Skip(expense.LastFeeValues.Length - 10).ToArray();
            }
        }

        await dbContext.SaveChangesAsync();
    }
}