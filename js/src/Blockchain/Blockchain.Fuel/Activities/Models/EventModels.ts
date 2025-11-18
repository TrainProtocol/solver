export interface TokenCommittedEvent {
    Id: string;
    dstChain: string;
    dstAddress: string;
    dstAsset: string;
    srcAsset: string;
    amount: string;
    timelock: string;
    srcReceiver: AddressBits;
    sender: AddressBits;
    assetId: AddressBits;
}

interface AddressBits {
    bits: string;
}

export interface TokenLockedEvent {
    hashlock: string;
    timelock: string;
    Id: string;
}