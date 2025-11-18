export interface TokenLockedEvent {
    hashlock: bigint;
    timelock: bigint;
    Id: bigint;
}

export interface TokenCommittedEvent {
    Id: bigint;
    dstChain: bigint;
    dstAsset: bigint;
    srcAsset: bigint;
    amount: bigint;
    timelock: bigint;
    tokenContract: string;
    srcReceiver: bigint;
    sender: bigint;
}
