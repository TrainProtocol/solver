using Microsoft.EntityFrameworkCore;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Data.Abstractions.Repositories;
using Train.Solver.Common.Enums;

namespace Train.Solver.Data.Npgsql;

public class EFFeeRepository(INetworkRepository networkRepository, SolverDbContext dbContext) : IFeeRepository
{
    public async Task<ServiceFee?> CreateServiceFeeAsync(
        string name,
        decimal feeInUsd,
        decimal percentageFee)
    {
        var serviceFee = new ServiceFee
        {
            Name = name,
            FeeInUsd = feeInUsd,
            FeePercentage = percentageFee
        };

        dbContext.ServiceFees.Add(serviceFee);
        await dbContext.SaveChangesAsync();

        return serviceFee;
    }

    public async Task<List<Expense>> GetExpensesAsync()
    {
        return await dbContext.Expenses
            .Include(x => x.FeeToken.TokenPrice)
            .Include(x => x.Token.TokenPrice)
            .ToListAsync();
    }

    public async Task<List<ServiceFee>> GetServiceFeesAsync()
    {
        return await dbContext.ServiceFees.ToListAsync();
    }

    public async Task<ServiceFee> GetServiceFeeAsync(string name)
    {
        var serviceFee = await dbContext.ServiceFees
            .FirstOrDefaultAsync(x => x.Name == name);

        if (serviceFee == null)
        {
            throw new Exception($"Service fee {name} not found");
        }

        return serviceFee;
    }

    public async Task<ServiceFee?> UpdateServiceFeeAsync(
        string name,
        decimal feeInUsd,
        decimal percentageFee)
    {
        var serviceFee = await GetServiceFeeAsync(name);

        if (serviceFee == null)
        {
            throw new Exception($"Service fee {name} not found");
        }

        serviceFee.FeePercentage = percentageFee;
        serviceFee.FeeInUsd = feeInUsd;

        await dbContext.SaveChangesAsync();

        return serviceFee;
    }

    public async Task UpdateExpenseAsync(
        string networkName, string tokenAsset, string feeAsset, string feeAmount, TransactionType transactionType)
    {
        var network = await networkRepository.GetAsync(networkName);

        if (network == null)
        {
            throw new Exception($"Network {networkName} not found");
        }

        var token = network.Tokens.FirstOrDefault(x => x.Asset == tokenAsset);

        if (token == null)
        {
            throw new Exception($"Token {tokenAsset} not found in network {networkName}");
        }

        var expense = await dbContext.Expenses.FirstOrDefaultAsync(x =>
            x.TokenId == token.Id && x.FeeTokenId == network.NativeTokenId && x.TransactionType == transactionType);

        if (expense == null)
        {
            expense = new Expense
            {
                TokenId = token.Id,
                FeeTokenId = network.NativeTokenId!.Value,
                TransactionType = transactionType
            };

            dbContext.Expenses.Add(expense);
        }

        expense.LastFeeValues = expense.LastFeeValues
            .Append(feeAmount)
            .TakeLast(10)
            .ToArray();

        await dbContext.SaveChangesAsync();
    }
}