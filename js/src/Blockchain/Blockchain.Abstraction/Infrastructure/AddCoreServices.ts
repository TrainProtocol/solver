import 'reflect-metadata';
import Redis from 'ioredis';
import Redlock from 'redlock';
import { container } from 'tsyringe';
import { ConvertToRedisUrl } from './RedisHelper/RedisFactory';
import { PrivateKeyService } from '../../Blockchain.Aztec/KeyVault/vault.service';
import { PrivateKeyConfigService } from '../../Blockchain.Aztec/KeyVault/vault.config';

export async function AddCoreServices(): Promise<void> {

  const redisUrl = ConvertToRedisUrl(process.env.TrainSolver__RedisConnectionString);
  const redis = new Redis(redisUrl);
  const redlock = new Redlock([redis], {
    retryCount: 5,
    retryDelay: 200,
    retryJitter: 100,
  });

  container.registerSingleton(PrivateKeyConfigService);
  container.registerSingleton(PrivateKeyService);

  container.register<Redlock>("Redlock", { useValue: redlock });
  container.register<Redis>("Redis", { useValue: redis });
}