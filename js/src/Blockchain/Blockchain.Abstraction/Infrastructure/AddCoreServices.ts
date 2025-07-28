import 'reflect-metadata';
import Redis from 'ioredis';
import Redlock from 'redlock';
import { container } from 'tsyringe';
import { ConvertToRedisUrl } from './RedisHelper/RedisFactory';

export async function AddCoreServices(): Promise<void> {
    const redisUrl = ConvertToRedisUrl(process.env.TrainSolver__RedisConnectionString);
    const redis = new Redis(redisUrl);
    const redlock = new Redlock([redis], {
        retryCount: 5,
        retryDelay: 200,
        retryJitter: 100,
    });

    container.register<Redlock>("Redlock", { useValue: redlock });
    container.register<Redis>("Redis", { useValue: redis });
}