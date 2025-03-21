export function decodeJson<T>(json: string): T {
  return JSON.parse(json) as T;
}
