using Train.Solver.Core.Entities;

namespace Train.Solver.Core.Repositories;

public interface IFeeRepository
{
    Task<List<Expense>> GetExpensesAsync();

    Task UpdateExpenseAsync(
        string networkName,
        string token,
        string feeToken,
        decimal fee,
        TransactionType transactionType);

    Task<List<ServiceFee>> GetServiceFeesAsync();
}
