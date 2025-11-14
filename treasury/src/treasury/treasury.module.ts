import { Module } from '@nestjs/common';
import { EvmTreasuryService as EvmTreasuryService } from './evm/evm.service';
import { TREASURIES } from './shared/constants.treasury';
import { StarknetTreasuryService as StarknetTreasuryService } from './starknet/starknet.service';
import { FuelTreasuryService } from './fuel/fuel.service';
import { VaultModule } from '../kv/vault.module';
import { AztecTreasuryService } from './aztec/aztec.service';
import { AztecConfigService } from './aztec/aztec.config';

@Module({
  imports: [VaultModule],
  providers: [
    EvmTreasuryService,
    StarknetTreasuryService,
    FuelTreasuryService,
    AztecTreasuryService,
    AztecConfigService,
    {
      provide: TREASURIES,
      useFactory: (evm, starknet, fuel, aztec) => [evm, starknet, fuel, aztec],
      inject: [EvmTreasuryService, StarknetTreasuryService, FuelTreasuryService],
    },
  ],
  exports: [TREASURIES],
})
export class TreasuryModule { }