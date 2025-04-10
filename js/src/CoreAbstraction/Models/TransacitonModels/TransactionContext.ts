import { NetworkType } from "../../../Data/Entities/Networks";
import { TransactionType } from "./TransactionType";

export interface TransactionContext {
    PrepareArgs: string;
    Type: TransactionType;
    NetworkName: string;
    NetworkType: NetworkType;
    FromAddress: string;
    SwapId: string;
  }