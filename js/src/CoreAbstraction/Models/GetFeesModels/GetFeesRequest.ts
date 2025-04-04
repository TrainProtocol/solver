export interface GetFeesRequest {
    NetworkName: string,
    ToAddress: string,
    Amount: number,
    FromAddress: string,
    Asset: string,
    CallData?: string
}