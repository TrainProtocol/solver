import { TransactionType } from "../Models/TransacitonModels/TransactionType";

export function decodeJson<T>(json: string): T {
    const parsed = JSON.parse(json);
    const lowerCamelCaseObj = convertKeysToCamelCase(parsed);
    return lowerCamelCaseObj as T;
}

export function removeHexPrefix(hex: string): string {
  return hex.startsWith('0x') ? hex.slice(2) : hex;
}

export function concatHexes(a: string, b: string): string {
  return removeHexPrefix(a) + removeHexPrefix(b);
}

export function hexToBigInt(hex: string): bigint {
  return BigInt("0x" + removeHexPrefix(hex));
}

export function BigIntToAscii(bigint: bigint): string {

  const hex = ToHex(bigint)
  let str = '';
  for (let i = 0; i < hex.length; i += 2) {
    const code = parseInt(hex.substr(i, 2), 16);
    if (code) str += String.fromCharCode(code);
  }
  return str;
}

export function parseHexToUTF8(hex: string): string {
  return Buffer.from(removeHexPrefix(hex), 'hex').toString('utf8');
}

export function ToHex(value: bigint): string {
  return '0x' + value.toString(16);
}

export function buildProcessorId(guid: string, networkName: string, type: TransactionType): string {
  return `${networkName}-${TransactionType[type]}-${guid}`;
}

function toLowerCamelCase(str) {
    return str.charAt(0).toLowerCase() + str.slice(1);
}

function convertKeysToCamelCase(obj) {
    const result = {};
    for (const key in obj) {
        if (Object.hasOwnProperty.call(obj, key)) {
            result[toLowerCamelCase(key)] = obj[key];
        }
    }
    return result;
}
