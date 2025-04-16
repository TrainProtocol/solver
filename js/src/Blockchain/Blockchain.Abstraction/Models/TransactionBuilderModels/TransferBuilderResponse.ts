export interface PrepareTransactionResponse {
    ToAddress: string;
    Data?: string;
    Amount: number;
    Asset?: string;
    AmountInWei: string;
    CallDataAsset: string;
    CallDataAmountInWei: string;
    CallDataAmount: number;
}