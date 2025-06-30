import { Module } from '@nestjs/common';
import { TreasuryModule } from './treasury/treasury.module';
import { TreasuryController } from './app/treasury.controller';
import { VaultModule } from './kv/vault.module';
import { ConfigModule } from '@nestjs/config';

@Module({
  imports: [TreasuryModule, VaultModule, ConfigModule.forRoot({ isGlobal: true })],
  controllers: [TreasuryController]
})
export class AppModule {}
