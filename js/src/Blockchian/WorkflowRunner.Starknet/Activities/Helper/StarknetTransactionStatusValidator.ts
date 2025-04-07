import { TransactionStatus } from "../../../../CoreAbstraction/Models/TransacitonModels/TransactionStatus";

export class StarknetTransactionStatusValidator {

    static TransferStatuses = {
        Confirmed: ["ACCEPTED_ON_L1", "ACCEPTED_ON_L2"],
        Failed: ["REJECTED"],
        Pending: ["NOT_RECEIVED", "RECEIVED"],
      };
    
      static ExecutionStatuses = {
        Confirmed: ["SUCCEEDED"],
        Failed: ["REVERTED"],
      };

    public static validateTransactionStatus(
        finalityStatus: string,
        executionStatus: string
      ): TransactionStatus {
        if (
          StarknetTransactionStatusValidator.TransferStatuses.Confirmed.includes(finalityStatus) &&
          StarknetTransactionStatusValidator.ExecutionStatuses.Confirmed.includes(executionStatus)
        ) {
          return TransactionStatus.Completed;
        }
      
        if (
          StarknetTransactionStatusValidator.TransferStatuses.Confirmed.includes(finalityStatus) &&
          StarknetTransactionStatusValidator.ExecutionStatuses.Failed.includes(executionStatus)
        ) {
          return TransactionStatus.Failed;
        }
      
        if (StarknetTransactionStatusValidator.TransferStatuses.Failed.includes(finalityStatus)) {
          return TransactionStatus.Failed;
        }
      
        if (StarknetTransactionStatusValidator.TransferStatuses.Pending.includes(finalityStatus)) {
          return TransactionStatus.Initiated;
        }
      
        throw new Error(
          `Transaction status not supported. finalityStatus: ${finalityStatus}, executionStatus: ${executionStatus}`
        );
      }
}