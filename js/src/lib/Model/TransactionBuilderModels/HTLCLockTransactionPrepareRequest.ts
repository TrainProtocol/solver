export interface HTLCLockTransactionPrepareRequest {
    Receiver: string;
    Hashlock: string;
    Timelock: number;
    SourceAsset: string;
    SourceNetwork: string;
    DestinationNetwork: string;
    DestinationAddress: string;
    DestinationAsset: string;
    Id: string;
    Amount: number;
    Reward: number;
    RewardTimelock: number;
  }