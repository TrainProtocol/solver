import { AztecSignTransactionRequest } from "../../../Blockchain.Abstraction/Infrastructure/TreasuryClient/Models";

export interface AztecSignTransactionRequestModel {
    networkType: string;
    signRequest: AztecSignTransactionRequest;
    signerAgentUrl: string;
}