export interface TransferBuilderRequestBase {
    CorrelationId: string;
    ReferenceId: string;
    ContractAddress: string;
    IsErc20: boolean;
    FunctionName: FunctionName;
}

export enum FunctionName
{
    Lock = "lock",
    Redeem = "redeem",
    Refund = "refund",
    AddLockSig = "addLockSig",
    Approve = "approve",
    Transfer = "transfer",
}