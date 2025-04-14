export const TimeSpan = {
    FromSeconds: (s: number) => s,
    FromMinutes: (m: number) => m * 60,
    FromHours: (h: number) => h * 60 * 60,
    FromDays: (d: number) => d * 24 * 60 * 60,
  };