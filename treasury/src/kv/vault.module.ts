import { Module } from "@nestjs/common";
import { PrivateKeyService } from "./vault.service";
import { PrivateKeyConfigService } from "./vault.config";
import { ConfigModule } from "@nestjs/config";

@Module({
  imports: [ConfigModule],
  providers: [PrivateKeyConfigService, PrivateKeyService],
  exports: [PrivateKeyService]
})
export class VaultModule {}
