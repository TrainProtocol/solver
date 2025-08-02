export interface NetworkDto {
    name: string;
    chainId: string;
    type: NetworkType;
}

export enum NetworkType {
    EVM,
    Solana,
    Starknet,
    Fuel,
    Aztec,
}