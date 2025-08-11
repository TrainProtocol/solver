export interface HTLCCommitTransactionPrepareRequest {
  receiver: string;
  hopChains: string[];
  hopAssets: string[];
  hopAddresses: string[];
  destinationChain: string;
  destinationAsset: string;
  destinationAddress: string;
  sourceAsset: string;
  timelock: number;
  amount: number;
  id: string;
}