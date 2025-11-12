import { Injectable } from "@nestjs/common";
import { ConfigService } from "@nestjs/config";

@Injectable()
export class AztecConfigService {
    constructor(private configService: ConfigService) {}

    get storePath(): string {
        return this.configService.get('AZTEC_STORE_PATH');
    }
} 
