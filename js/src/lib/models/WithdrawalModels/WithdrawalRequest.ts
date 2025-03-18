import { FixedFeeData } from "../GetFeesModels/GetFeesResponse";

export interface WithdrawalRequest {
    FromAddress: string;
    Network: string;
    TokenContract?: string;
    ChainId?: string;
    Decimals?: number;
    Nonce?: string;
    CorrelationId: string;
    ReferenceId?: string;
    NodeUrl?: string;
    CallData?: string;
    Fee?: FixedFeeData;
}