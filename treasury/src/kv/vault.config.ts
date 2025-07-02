import { Injectable } from "@nestjs/common";
import { ConfigService } from "@nestjs/config";

@Injectable()
export class PrivateKeyConfigService {
    static k8sMountPoint: string = 'kubernetes';
    static userPassMountPoint: string = 'userpass';

    constructor(private configService: ConfigService) {}

    get isK8sAuthEnabled(): boolean {
        return this.configService.get('VAULT_K8S_AUTH_ENABLED') === 'true';
    }

    get k8sRoleName(): string {
        return this.configService.get('VAULT_K8S_ROLE_NAME');
    }

    get k8sTokenPath(): string {
        return this.configService.get('VAULT_K8S_SERVICE_ACCOUNT_TOKEN_PATH');
    }

    get url(): string {
        return this.configService.get('VAULT_URL');
    }

    get username(): string {
        return this.configService.get('VAULT_USERNAME');
    }

    get password(): string {
        return this.configService.get('VAULT_PASSWORD');
    }

    get mountPath(): string {
        return this.configService.get('VAULT_MOUNT_PATH') ?? 'secret';
    }
} 