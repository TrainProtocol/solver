export interface WithdrawalRequest {
    FromAddress: string;
    ToAddress: string;
    Asset: string;
    Amount: number;
    Network: string;
    TokenContract?: string;
    ChainId?: string;
    Decimals?: number;
    Nonce?: string;
    CorrelationId: string;
    ReferenceId?: string;
    EthereumNodeUrl?: string;
    NodeUrl?: string;
    CallData?: string;
    FeeAsset?: string;
    FeeTokenContract?: string;
    FeeAmountInWei?: string;
}