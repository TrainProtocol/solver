export type GetFeesResponse = Record<string, Fee>

export interface Fee
{
    Asset: string,
    Decimals: number,
    FixedFeeData: FixedFeeData,
    LegacyFeeData: LegacyFeeData,
}

export interface FixedFeeData {
    FeeInWei: string;
}

export interface LegacyFeeData {
    GasPriceInWei: string,
    GasLimit: string,
    L1FeeInWei: string,
}