using Train.Solver.Data.Abstractions.Entities;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IFeeRepository
{
    Task<List<Expense>> GetExpensesAsync();

    Task UpdateExpenseAsync(
        string networkName,
        string token,
        string feeToken,
        string fee,
        TransactionType transactionType);

    Task<List<ServiceFee>> GetServiceFeesAsync();
}
