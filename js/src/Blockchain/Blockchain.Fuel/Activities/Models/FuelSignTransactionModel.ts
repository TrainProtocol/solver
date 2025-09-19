import { FuelSignTransactionRequestModel } from "../../../Blockchain.Abstraction/Infrastructure/TreasuryClient/Models";

export interface SignTransactionRequest {
    networkType: string;
    signRequest: FuelSignTransactionRequestModel;
    signerAgentUrl: string;
}