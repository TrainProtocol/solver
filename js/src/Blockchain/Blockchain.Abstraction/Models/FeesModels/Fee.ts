export interface Fee
{
    Asset: string,
    FixedFeeData: FixedFeeData,
    LegacyFeeData?: LegacyFeeData,
}

export interface FixedFeeData {
    FeeInWei: string;
}

export interface LegacyFeeData {
    GasPriceInWei: string,
    GasLimit: string,
    L1FeeInWei: string,
}