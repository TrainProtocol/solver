import { Module } from '@nestjs/common';
import { EvmTreasuryService as EvmTreasuryService } from './evm/evm.service';
import { TREASURIES } from './shared/constants.treasury';
import { StarknetTreasuryService as StarknetTreasuryService } from './starknet/starknet.service';
import { FuelTreasuryService } from './fuel/fuel.service';
import { VaultModule } from '../kv/vault.module';
import { AztecTreasuryService } from './aztec/aztec.service';
import { AztecConfigService } from './aztec/aztec.config';
import { SolanaTreasuryService } from './solana/solana.service';

@Module({
  imports: [VaultModule],
  providers: [
    EvmTreasuryService,
    StarknetTreasuryService,
    FuelTreasuryService,
    AztecTreasuryService,
    AztecConfigService,
    SolanaTreasuryService,
    {
      provide: TREASURIES,
      useFactory: (evm, starknet, fuel) => [evm, starknet, fuel],
      inject: [EvmTreasuryService, StarknetTreasuryService, FuelTreasuryService],
    },
  ],
  exports: [TREASURIES],
})
export class TreasuryModule { }