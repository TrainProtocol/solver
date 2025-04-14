export const TimeSpan = {
  FromMilliseconds: (ms: number) => ms,
  FromSeconds: (s: number) => s * 1000,
  FromMinutes: (m: number) => m * 60 * 1000,
  FromHours: (h: number) => h * 60 * 60 * 1000,
  FromDays: (d: number) => d * 24 * 60 * 60 * 1000,
};