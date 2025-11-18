import 'reflect-metadata';
import Redis from 'ioredis';
import Redlock from 'redlock';
import { container } from 'tsyringe';
import { ConvertToRedisUrl } from './RedisHelper/RedisFactory';
import { PrivateKeyService } from '../../Blockchain.Aztec/KeyVault/vault.service';
import { PrivateKeyConfigService } from '../../Blockchain.Aztec/KeyVault/vault.config';
import { AztecConfigService } from '../../Blockchain.Aztec/KeyVault/aztec.config';

export async function AddCoreServices(): Promise<void> {

  const redisUrl = ConvertToRedisUrl(process.env.TrainSolver__RedisConnectionString);
  const redis = new Redis(redisUrl);
  const redlock = new Redlock([redis], {
    retryCount: 5,
    retryDelay: 200,
    retryJitter: 100,
  });
  const configService = {
    get: (key: string) => process.env[key]
  };

  const privateKeyConfigService = new PrivateKeyConfigService(configService as any);
  const privateKeyService = new PrivateKeyService();
  
  privateKeyService.init(privateKeyConfigService);
  const aztecConfigService = new AztecConfigService(configService as any);

  container.register<Redlock>("Redlock", { useValue: redlock });
  container.register<Redis>("Redis", { useValue: redis });
  container.register<PrivateKeyService>("PrivateKeyService", { useValue: privateKeyService });
  container.register<AztecConfigService>("AztecConfigService", { useValue: aztecConfigService });
}
