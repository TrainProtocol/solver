export const Delimiter = ":";

export function BuildLockKey(network: string, address: string, asset?: string): string {
    return asset
        ? `${network}${Delimiter}${address}${Delimiter}${asset}`
        : `${network}${Delimiter}${address}`;
}

export function BuildNonceKey(network: string, address: string, asset?: string): string {
    return asset
        ? `${network}${Delimiter}${address}${Delimiter}${asset}${Delimiter}currentNonce`
        : `${network}${Delimiter}${address}${Delimiter}currentNonce`;
}