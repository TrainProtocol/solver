import { Module } from '@nestjs/common';
import { TreasuryModule } from './treasury/treasury.module';
import { TreasuryController } from './app/treasury.controller';
import { VaultModule } from './kv/vault.module';
import { ConfigModule } from '@nestjs/config';
import { HealthController } from './app/health.controller';

@Module({
  imports: [TreasuryModule, VaultModule, ConfigModule.forRoot({ isGlobal: true })],
  controllers: [TreasuryController, HealthController],
})
export class AppModule {}
