export interface GetFeesRequest 
{
    CorrelationId: string,
    Decimals: number,
    FromAddress: string,
    NodeUrl: string,
    Symbol: string,
    TokenContract: string,
    CallData?: string
}