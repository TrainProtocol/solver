export interface HTLCLockTransactionPrepareRequest {
    receiver: string;
    hashlock: string;
    timelock: number;
    sourceAsset: string;
    sourceNetwork: string;
    destinationNetwork: string;
    destinationAddress: string;
    destinationAsset: string;
    commitId: string;
    amount: number;
    reward: number;
    rewardTimelock: number;
  }