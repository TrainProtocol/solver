import { IBlockchainActivities } from "../Blockchain/Blockchain.Abstraction/Interfaces/IBlockchainActivities";

export function extractActivities<T extends IBlockchainActivities>(instance: T): Record<keyof T, any> {
  const activities = {} as Record<keyof T, any>;

  const proto = Object.getPrototypeOf(instance);
  const methodNames = Object.getOwnPropertyNames(proto)
    .filter(name => typeof instance[name as keyof T] === 'function' && name !== 'constructor');

  for (const methodName of methodNames) {
    activities[methodName as keyof T] = (instance[methodName as keyof T] as Function).bind(instance);
  }

  return activities;
}