export interface PrepareTransactionResponse {
    ToAddress: string;
    Data?: string;
    Amount: Number;
    Asset?: string;
    AmountInWei: string;
    CallDataAsset: string;
    CallDataAmountInWei: string;
    CallDataAmount: Number;
}