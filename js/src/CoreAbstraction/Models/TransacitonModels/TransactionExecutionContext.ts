import { Fee } from "../GetFeesModels/GetFeesResponse";

export interface TransactionExecutionContext {
    Attempts: number;
    Fee?: Fee;
    Nonce?: string;
    PublishedTransactionIds: string[];
}