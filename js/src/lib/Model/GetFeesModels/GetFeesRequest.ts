export interface GetFeesRequest {
    ToAddress: string,
    amount: number,
    FromAddress: string,
    Asset: string,
    CallData?: string
}