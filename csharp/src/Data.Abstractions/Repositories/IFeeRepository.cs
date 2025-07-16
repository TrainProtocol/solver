using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Util.Enums;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IFeeRepository
{
    Task<List<Expense>> GetExpensesAsync();

    Task<List<ServiceFee>> GetServiceFeesAsync();

    Task<ServiceFee?> CreateServiceFeeAsync(decimal feeInUsd, decimal percentageFee);

    Task UpdateExpenseAsync(
        string networkName,
        string token,
        string feeToken,
        string fee,
        TransactionType transactionType);
}
