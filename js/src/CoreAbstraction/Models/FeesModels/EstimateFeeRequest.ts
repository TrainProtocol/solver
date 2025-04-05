export interface EstimateFeeRequest {
    NetworkName: string,
    ToAddress: string,
    Amount: number,
    FromAddress: string,
    Asset: string,
    CallData?: string
}