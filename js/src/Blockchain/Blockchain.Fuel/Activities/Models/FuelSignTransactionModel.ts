import { FuelSignTransactionRequest } from "../../../Blockchain.Abstraction/Infrastructure/TreasuryClient/Models";

export interface FuelSignTransactionRequestModel {
    networkType: string;
    signRequest: FuelSignTransactionRequest;
    signerAgentUrl: string;
}