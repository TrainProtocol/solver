import { Fee } from "../FeesModels/Fee";

export interface TransactionExecutionContext {
    Attempts: number;
    Fee?: Fee;
    Nonce?: string;
    PublishedTransactionIds: string[];
}