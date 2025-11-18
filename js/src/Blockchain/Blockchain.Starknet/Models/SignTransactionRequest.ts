import { StarknetSignTransactionRequestModel } from "../../Blockchain.Abstraction/Infrastructure/TreasuryClient/Models/StarknetSignTransactionRequestModel";

export interface SignTransactionRequest{
    signRequest: StarknetSignTransactionRequestModel;
    networkType: string;
    signerAgentUrl: string;
}