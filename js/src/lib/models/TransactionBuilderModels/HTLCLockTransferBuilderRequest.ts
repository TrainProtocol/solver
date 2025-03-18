import { TransferBuilderRequestBase } from "./TransferBuilderRequestBase";

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