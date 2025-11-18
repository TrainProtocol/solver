import { AztecSignTransactionRequest } from "../../../Blockchain.Abstraction/Infrastructure/TreasuryClient/Models/AztecSignTransactionRequest";

export interface AztecSignTransactionRequestModel {
    networkType: string;
    tokenContract: string;
    contractAddress: string;
    solverAddress: string;
    nodeUrl: string;
    unsignedTxn: string;
}