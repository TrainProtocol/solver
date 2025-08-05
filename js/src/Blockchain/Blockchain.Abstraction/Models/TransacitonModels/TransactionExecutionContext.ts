import { Fee } from "../FeesModels/Fee";

export interface TransactionExecutionContext {
    attempts?: number;
    fee?: Fee;
    nonce?: string;
    publishedTransactionIds?: string[];
}