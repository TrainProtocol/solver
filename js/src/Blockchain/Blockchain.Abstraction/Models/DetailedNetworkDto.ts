import { NetworkDto } from "./Dtos/NetworkDto";
import { NodeDto } from "./Dtos/NodeDto";
import { TokenDto } from "./Dtos/TokenDto";


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