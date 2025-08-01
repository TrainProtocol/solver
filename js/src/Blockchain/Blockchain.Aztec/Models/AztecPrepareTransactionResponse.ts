import { PrepareTransactionResponse } from "../../Blockchain.Abstraction/Models/TransactionBuilderModels/TransferBuilderResponse";

export interface AztecPrepareTransactionResponse extends PrepareTransactionResponse {
    functionInteractions?: any[];
}
