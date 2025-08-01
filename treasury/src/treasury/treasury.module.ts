import { Module } from '@nestjs/common';
import { EvmTreasuryService as EvmTreasuryService } from './evm/evm.service';
import { TREASURIES } from './shared/constants.treasury';
import { StarknetTreasuryService as StarknetTreasuryService } from './starknet/starknet.service';
import { VaultModule } from 'src/kv/vault.module';
import { FuelTreasuryService } from './fuel/fuel.service';

@Module({
  imports: [VaultModule],
  providers: [
    EvmTreasuryService,
    StarknetTreasuryService,
    FuelTreasuryService,
    {
      provide: TREASURIES,
      useFactory: (evm, starknet, fuel) => [evm, starknet, fuel],
      inject: [EvmTreasuryService, StarknetTreasuryService, FuelTreasuryService],
    },
  ],
  exports: [TREASURIES],
  })
export class TreasuryModule {}