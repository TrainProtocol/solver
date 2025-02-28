export interface SufficientBalanceRequest {
    Address: string;
    Symbol: string;
    CorrelationId: string;
    NodeUrl: string;
    EthereumNodeUrl?: string;
    TokenContract: string;
    Decimals: number;
    Amount: number;
}
