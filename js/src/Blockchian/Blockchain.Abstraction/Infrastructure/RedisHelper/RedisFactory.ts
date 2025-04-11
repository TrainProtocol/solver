export function ConvertToRedisUrl(connectionString: string, db: number = 3): string {
  const [hostPart, ...optionParts] = connectionString.split(',');

  const [host, portString] = hostPart.split(':');
  const port = Number(portString);

  const options: Record<string, string> = {};
  for (const part of optionParts) {
      const [key, ...valueParts] = part.split('=');
      options[key.trim().toLowerCase()] = valueParts.join('=').trim();
  }

  const password = options['password'];
  const ssl = options['ssl']?.toLowerCase() === 'true';

  if (!host || !port || !password) {
      throw new Error(`Invalid connection string. Parsed values: host=${host}, port=${port}, password=${!!password}`);
  }

  const protocol = ssl ? 'rediss' : 'redis';
  const encodedPassword = encodeURIComponent(password);

  return `${protocol}://:${encodedPassword}@${host}:${port}/${db}`;
}