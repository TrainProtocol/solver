export interface HTLCLockTransferBuilderRequest extends TransferBuilderRequestBase {
    Amount: Number;
    AmountInWei: string;
    Reward: Number;
    RewardInWei: string;
    RewardTimelock: string;
    Hashlock: string;
    Timelock: string;
    Receiver: string;
    SourceAsset: string;
    DestinationChain: string;
    DestinationAddress: string;
    DestinationAsset: string;
    Id: string;
    TokenContract: string;
}

export enum FunctionName
{
    Lock = "lock",
    Redeem = "redeem",
    Refund = "refund",
    AddLockSig = "addLockSig",
    Approve = "approve",
}

export interface TransferBuilderRequestBase {
    CorrelationId: string;
    ReferenceId: string;
    HTLCContractAddress: string;
    IsErc20: boolean;
    FunctionName: FunctionName;
}