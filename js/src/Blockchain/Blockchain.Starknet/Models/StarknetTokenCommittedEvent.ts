export interface TokenCommittedEvent {
    Id: bigint;
    HopChains: string[];
    HopAssets: string[];
    HopAddress: string[];
    DestinationNetwork: string;
    DestinationAddress: string;
    DestinationAsset: string;
    SourceAsset: string;
    AmountInBaseUnits: string;
    Timelock: bigint;
    TokenContract: string;
    SourceReciever: string;
    SenderAddress: string;
  }
  