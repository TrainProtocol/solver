export interface TransferBuilderResponse {
    ToAddress: string;
    Data?: string;
    Amount: Number;
    Asset?: string;
    AmountInWei: string;
    CallDataAsset: string;
    CallDataAmountInWei: string;
    CallDataAmount: Number;
}