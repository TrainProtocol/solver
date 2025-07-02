export interface HTLCCommitTransactionPrepareRequest {
  Receiever: string;
  HopChains: string[];
  HopAssets: string[];
  HopAddresses: string[];
  DestinationChain: string;
  DestinationAsset: string;
  DestinationAddress: string;
  SourceAsset: string;
  Timelock: number;
  Amount: number;
}