import { ArraySignatureType, BigNumberish, ResourceBounds, Signature } from "starknet";

export function signatureToHexArray(sig?: Signature): ArraySignatureType {
  return bigNumberishArrayToHexadecimalStringArray(formatSignature(sig));
}

export function formatSignature(sig?: Signature): ArraySignatureType {
  if (!sig) throw Error('formatSignature: provided signature is undefined');
  if (Array.isArray(sig)) {
    return sig.map((it) => toHex(it));
  }
  try {
    const { r, s } = sig;
    return [toHex(r), toHex(s)];
  } catch (e) {
    throw new Error('Signature need to be weierstrass.SignatureType or an array for custom');
  }
}

export function bigNumberishArrayToHexadecimalStringArray(data: BigNumberish[]): string[] {
  return data.map((x) => toHex(x));
}

export function toHex(value: BigNumberish): string {
  return addHexPrefix(toBigInt(value).toString(16));
}

export function addHexPrefix(hex: string): string {
  return `0x${removeHexPrefix(hex)}`;
}

export function removeHexPrefix(hex: string): string {
  return hex.startsWith('0x') || hex.startsWith('0X') ? hex.slice(2) : hex;
}

export function toBigInt(value: BigNumberish): bigint {
  return BigInt(value);
}

export function resourceBoundsToHexString(resourceBoundsBN: ResourceBounds): ResourceBounds {
  const convertBigIntToHex = (obj: any): any => {
    if (isBigInt(obj)) {
      return toHex(obj);
    }
    
    return obj;
  };

  return convertBigIntToHex(resourceBoundsBN) as ResourceBounds;
}

export function isBigInt(value: any): value is bigint {
  return typeof value === 'bigint';
}