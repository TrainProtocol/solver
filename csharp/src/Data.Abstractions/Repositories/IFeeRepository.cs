using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Common.Enums;
using Train.Solver.Data.Abstractions.Models;

namespace Train.Solver.Data.Abstractions.Repositories;

public interface IFeeRepository
{
    Task<List<Expense>> GetExpensesAsync();

    Task<List<ServiceFee>> GetServiceFeesAsync();

    Task<ServiceFee> GetServiceFeeAsync(string name);

    Task<ServiceFee?> CreateServiceFeeAsync(CreateServiceFeeRequest request);

    Task<ServiceFee?> UpdateServiceFeeAsync(string name, UpdateServiceFeeRequest request);

    Task UpdateExpenseAsync(
        string networkName,
        string token,
        string feeToken,
        string fee,
        TransactionType transactionType);
}
