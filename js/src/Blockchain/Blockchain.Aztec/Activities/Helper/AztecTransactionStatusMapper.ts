import { TxStatus } from "@aztec/aztec.js";
import { TransactionStatus } from '../../../Blockchain.Abstraction/Models/TransacitonModels/TransactionStatus';

export function mapAztecStatusToInternal(status: TxStatus): TransactionStatus {
    switch (status) {
        case TxStatus.SUCCESS:
            return TransactionStatus.Completed;

        case TxStatus.PENDING:
            return TransactionStatus.Initiated;

        case TxStatus.DROPPED:
        case TxStatus.APP_LOGIC_REVERTED:
        case TxStatus.BOTH_REVERTED:
        case TxStatus.TEARDOWN_REVERTED:
            return TransactionStatus.Failed;

        default:
            throw new Error(`Unhandled Aztec transaction status: ${status}`);
    }
}