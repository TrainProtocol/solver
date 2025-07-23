import { NetworkDto } from "./NetworkDto";
import { NodeDto } from "./NodeDto";
import { TokenDto } from "./TokenDto";


export interface DetailedNetworkDto extends NetworkDto {
  displayName: string;
  htlcNativeContractAddress: string;
  htlcTokenContractAddress: string;
  feeType: TransactionFeeType;
  feePercentageIncrease: number;
  nativeToken?: TokenDto;
  tokens: TokenDto[];
  nodes: NodeDto[];
}

export enum TransactionFeeType {
  Default,
  EIP1559,
  ArbitrumEIP1559,
  OptimismEIP1559,
}

