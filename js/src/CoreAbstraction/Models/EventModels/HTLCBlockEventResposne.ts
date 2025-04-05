import { NetworkType } from "../../../Data/Entities/Networks";

export interface HTLCBlockEventResponse {
    HTLCCommitEventMessages: HTLCCommitEventMessage[];
    HTLCLockEventMessages: HTLCLockEventMessage[];
  }
  
  export interface HTLCCommitEventMessage {
    TxId: string;
    Id: string;
    Amount: number;
    AmountInWei: string;
    ReceiverAddress: string;
    SourceNetwork: string;
    SenderAddress: string;
    SourceAsset: string;
    DestinationAddress: string;
    DestinationNetwork: string;
    DestinationAsset: string;
    TimeLock: number;
    DestinationNetworkType: NetworkType;
    SourceNetworkType: NetworkType;
  }
  
  export interface HTLCLockEventMessage {
    TxId: string;
    Id: string;
    HashLock: string;
    TimeLock: number;
  }