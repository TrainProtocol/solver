namespace Train.Solver.Core.Blockchain.Starknet.Helpers;

public static class StarknetConstants
{
    public static class EventIds
    {
        public const string TransferEventId = "0x99cd8bde557814842a3121e8ddfd433a539b8c9f14bf31ebf108d12e6196e9";
        public const string HTLCLockEventId = "0x63b274f158ac6d7894b7102178639ec637af9d48a27cf8b8668bcc029bab5";
        public const string HTLCCommitEventId = "0x333f79676745d769cdc06da737bac7d2b0167a1297ecc53a526669f83990627";
    }

    public static class RpcMethods
    {
        public const string BlockNumber = "starknet_blockNumber";
        public const string GetEvents = "starknet_getEvents";
        public const string GetReceipt = "starknet_getTransactionReceipt";
        public const string GetBlockWithHashes = "starknet_getBlockWithTxHashes";
        public const string GetNonce = "starknet_getNonce";
        public const string GetTransactionStatus = "starknet_getTransactionStatus";
        public const string StarknetCall = "starknet_call";
    }

    public static class TransferStatuses
    {
        public static readonly string[] Confirmed = { "ACCEPTED_ON_L1", "ACCEPTED_ON_L2" };
        public static readonly string[] Failed = { "REJECTED" };
        public static readonly string[] Pending = { "NOT_RECEIVED", "RECEIVED" };
    }

    public static class ExecutionStatuses
    {
        public static readonly string[] Confirmed = { "SUCCEEDED" };
        public static readonly string[] Failed = { "REVERTED" };
    }
}
