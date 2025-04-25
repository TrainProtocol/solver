import { TransactionStatus } from "../../../../../Common/Abstraction/Models/TransacitonModels/TransactionStatus";

const TransferStatuses = {
    Confirmed: ["ACCEPTED_ON_L1", "ACCEPTED_ON_L2"],
    Failed: ["REJECTED"],
    Pending: ["NOT_RECEIVED", "RECEIVED"],
}

const ExecutionStatuses = {
    Confirmed: ["SUCCEEDED"],
    Failed: ["REVERTED"],
}

export function validateTransactionStatus(
    finalityStatus: string,
    executionStatus: string
): TransactionStatus {
    if (
        TransferStatuses.Confirmed.includes(finalityStatus) &&
        ExecutionStatuses.Confirmed.includes(executionStatus)
    ) {
        return TransactionStatus.Completed;
    }

    if (
        TransferStatuses.Confirmed.includes(finalityStatus) &&
        ExecutionStatuses.Failed.includes(executionStatus)
    ) {
        return TransactionStatus.Failed;
    }

    if (TransferStatuses.Failed.includes(finalityStatus)) {
        return TransactionStatus.Failed;
    }

    if (TransferStatuses.Pending.includes(finalityStatus)) {
        return TransactionStatus.Initiated;
    }

    throw new Error(
        `Transaction status not supported. finalityStatus: ${finalityStatus}, executionStatus: ${executionStatus}`
    );
}
