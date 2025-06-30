import { Module } from '@nestjs/common';
import { EvmTreasuryService as EvmTreasuryService } from './evm/evm.service';
import { TREASURIES } from './shared/constants.treasury';
import { StarknetTreasuryService as StarknetTreasuryService } from './starknet/starknet.service';
import { VaultModule } from 'src/kv/vault.module';

@Module({
  imports: [VaultModule],
  providers: [
    EvmTreasuryService,
    StarknetTreasuryService,
    {
      provide: TREASURIES,
      useFactory: (evm, starknet) => [evm, starknet],
      inject: [EvmTreasuryService, StarknetTreasuryService],
    },
  ],
  exports: [TREASURIES],
  })
export class TreasuryModule {}