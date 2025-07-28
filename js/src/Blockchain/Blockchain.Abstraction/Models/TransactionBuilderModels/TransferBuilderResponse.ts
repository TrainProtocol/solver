export interface PrepareTransactionResponse {
    toAddress: string;
    data?: string;
    amount: number;
    asset?: string;
    callDataAsset: string;
    callDataAmount: number;
}