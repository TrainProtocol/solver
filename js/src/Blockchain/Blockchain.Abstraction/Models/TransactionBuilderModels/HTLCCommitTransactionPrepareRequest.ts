export interface HTLCCommitTransactionPrepareRequest {
  Receiver: string;
  HopChains: string[];
  HopAssets: string[];
  HopAddresses: string[];
  DestinationChain: string;
  DestinationAsset: string;
  DestinationAddress: string;
  SourceAsset: string;
  Timelock: number;
  Amount: number;
  Id: string;
}