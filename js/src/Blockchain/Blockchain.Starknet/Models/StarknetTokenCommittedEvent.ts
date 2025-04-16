export interface TokenCommittedEvent {
    Id: bigint;
    dstChain: bigint;
    dstAddress: string;
    dstAsset: bigint;
    srcAsset: bigint;
    amount: bigint;
    timelock: bigint;
    tokenContract: string;
    srcReceiver: bigint;
    sender: bigint;
  }
  