export function ParseNonces(errorMessage: string): {
  expectedNonce: BigInt;
  providedNonce: BigInt;
} {

  if (!errorMessage) {
    return null;
  }

  const regex = /Account nonce: (0x[0-9a-fA-F]+).*got: (0x[0-9a-fA-F]+)/;

  const match = errorMessage.match(regex);

  if (!match) {
    return null;
  }

  const expectedNonceHex = match[1];
  const providedNonceHex = match[2];

  return {
    expectedNonce: BigInt(expectedNonceHex),
    providedNonce: BigInt(providedNonceHex)
  };
}