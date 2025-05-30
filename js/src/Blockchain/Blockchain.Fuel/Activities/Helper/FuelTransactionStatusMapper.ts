import { TransactionStatus as FuelTransactionStatus } from 'fuels';
import { TransactionStatus } from '../../../Blockchain.Abstraction/Models/TransacitonModels/TransactionStatus';

export function mapFuelStatusToInternal(status: FuelTransactionStatus): TransactionStatus {
  switch (status) {
    case FuelTransactionStatus.success:
      return TransactionStatus.Completed;

    case FuelTransactionStatus.submitted:
      return TransactionStatus.Initiated;

    case FuelTransactionStatus.failure:
    case FuelTransactionStatus.squeezedout:
      return TransactionStatus.Failed;

    default:
      throw new Error(`Unhandled Fuel transaction status: ${status}`);
  }
}