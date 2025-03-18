export interface GetAllowanceRequest 
{
    NodeUrl: string,
    TokenContract: string,
    OwnerAddress: string,
    SpenderAddress: string,
    Decimals: number
}