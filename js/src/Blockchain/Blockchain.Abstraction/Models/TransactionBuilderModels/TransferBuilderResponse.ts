export interface PrepareTransactionResponse {
    toAddress: string;
    data?: string;
    amount: string;
    asset?: string;
    callDataAsset: string;
    callDataAmount: string;
}