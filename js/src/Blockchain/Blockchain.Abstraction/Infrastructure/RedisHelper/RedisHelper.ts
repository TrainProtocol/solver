export const Delimiter = ":";

export function buildLockKey(network: string, address: string, asset?: string): string {
    return asset
        ? `${network}${Delimiter}${address}${Delimiter}${asset}`
        : `${network}${Delimiter}${address}`;
}

export function buildCurrentNonceKey(network: string, address: string, asset?: string): string {
    return asset
        ? `${network}${Delimiter}${address}${Delimiter}${asset}${Delimiter}currentNonce`
        : `${network}${Delimiter}${address}${Delimiter}currentNonce`;
}

export function buildNextNonceKey(network: string, address: string, asset?: string): string {
    return asset
        ? `${network}${Delimiter}${address}${Delimiter}${asset}${Delimiter}nextNonce`
        : `${network}${Delimiter}${address}${Delimiter}nextNonce`;
}