import { IsNotEmpty, IsString } from "class-validator";

export class BaseSignRequest {
    @IsNotEmpty()
    @IsString()
    readonly address: string;

    @IsNotEmpty()
    @IsString()
    readonly unsignedTxn: string;
}
  
export class BaseSignResponse {
    readonly signedTxn: string;
}

export class GenerateResponse {
    readonly address: string;
}