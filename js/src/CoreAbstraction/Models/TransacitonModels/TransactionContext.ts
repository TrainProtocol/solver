import { NetworkType } from "../../../Data/Entities/Networks";
import { Fee } from "../FeesModels/Fee";
import { TransactionType } from "./TransactionType";

export interface TransactionContext {
    PrepareArgs: string;
    Type: TransactionType;
    NetworkName: string;
    NetworkType: NetworkType;
    FromAddress: string;
    SwapId: string;
    Attempts: number;
    Fee?: Fee;
    Nonce?: string;
    PublishedTransactionIds: string[];
  }