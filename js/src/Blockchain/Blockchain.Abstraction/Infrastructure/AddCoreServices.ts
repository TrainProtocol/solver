import 'reflect-metadata';
import Redis from 'ioredis';
import Redlock from 'redlock';
import { container } from 'tsyringe';
import { ConvertToRedisUrl } from './RedisHelper/RedisFactory';
import { TreasuryClient } from './TreasuryClient/treasuryClient';

export async function AddCoreServices(): Promise<void> {

  const redisUrl = ConvertToRedisUrl(process.env.TrainSolver__RedisConnectionString);
  const redis = new Redis(redisUrl);
  const redlock = new Redlock([redis], {
    retryCount: 5,
    retryDelay: 200,
    retryJitter: 100,
  });
  const treasuryClient = new TreasuryClient();

  container.register<Redlock>("Redlock", { useValue: redlock });
  container.register<Redis>("Redis", { useValue: redis });
  container.register<TreasuryClient>("TreasuryClient", { useValue: treasuryClient });
}