export interface HTLCBlockEventResponse {
  htlcCommitEventMessages: HTLCCommitEventMessage[];
  htlcLockEventMessages: HTLCLockEventMessage[];
}

export interface HTLCCommitEventMessage {
  txId: string;
  commitId: string;
  amount: string;
  receiverAddress: string;
  sourceNetwork: string;
  senderAddress: string;
  sourceAsset: string;
  destinationAddress: string;
  destinationNetwork: string;
  destinationAsset: string;
  timeLock: number;
  tokenContract?: string;
}

export interface HTLCLockEventMessage {
  txId: string;
  commitId: string;
  hashLock: string;
  timeLock: number;
}