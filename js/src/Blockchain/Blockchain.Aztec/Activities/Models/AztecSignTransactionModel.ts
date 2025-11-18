import { AztecSignTransactionRequest } from "../../../Blockchain.Abstraction/Infrastructure/TreasuryClient/Models/AztecSignTransactionRequest";

export interface AztecSignTransactionRequestModel {
    networkType: string;
    signRequest: AztecSignTransactionRequest;
    signerAgentUrl: string;
}