import { ScriptTransactionRequest, WalletLocked } from "fuels";
import { BaseRequest } from "../../Blockchain.Abstraction/Models/BaseRequest";

export interface FuelSufficientBalanceRequest extends BaseRequest
{
    rawData : ScriptTransactionRequest;
    wallet : WalletLocked;
    callDataAsset : string;
    callDataAmount?: number;
}