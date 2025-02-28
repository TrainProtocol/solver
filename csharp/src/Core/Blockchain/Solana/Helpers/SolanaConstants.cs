namespace Train.Solver.Core.Blockchain.Solana.Helpers;

public static class SolanaConstants
{
    public static readonly Dictionary<string, decimal> MediumComputeUnitPrice = new()
    {
        { "SOLANA_DEVNET", 1 },
        { "SOLANA_MAINNET", 1 },
    };

    public static readonly Dictionary<string, decimal> HighComputeUnitPrice = new()
    {
        { "SOLANA_DEVNET", 10 },
        { "SOLANA_MAINNET", 10 },
    };

    public const int MicroLamportsDecimal = 6;
    public const int BaseLimit = 25000;
    public const int MaxComputeLimit = 1400000;

    public const string depositForBurnSighash = "global:deposit_for_burn";
    public const string reclaimSighash = "global:reclaim_event_account";
    public const string receiveMessageSighash = "global:receive_message";
    public const string nonceSighash = "global:get_nonce_pda";

    public const string memoType = "spl-memo";
    public static string[] transferTypes = { "transfer", "transferChecked" };

    public static readonly Dictionary<string, byte[]> LockDescriminator = new()
    {
        { "SOLANA_DEVNET", [ 21, 19, 208, 43, 237, 62, 255, 87 ] },
    };

    public static readonly Dictionary<string, byte[]> RefundDescriminator = new()
    {
        { "SOLANA_DEVNET", [ 2, 96, 183, 251, 63, 208, 46, 46 ] },
    };

    public static readonly Dictionary<string, byte[]> RedeemDescriminator = new()
    {
        { "SOLANA_DEVNET", [ 184, 12, 86, 149, 70, 196, 97, 225 ] },
    };

    public static readonly Dictionary<string, byte[]> GetDetailsDescriminator = new()
    {
        { "SOLANA_DEVNET", [ 185, 254, 236, 165, 213, 30, 224, 250 ] },
    };

    public static class HtlcConstants
    {
        public const string commitEventPrefixPattern = "Program log: Instruction: Commit";
        public const string addLockEventPrefixPattern = "Program log: Instruction: AddLock";

        public const string destinationAddressLogPrefixPattern = "dst_address:";
        public const string destinationAssetLogPrefixPattern = "dst_asset:";
        public const string destinationNetworkLogPrefixPattern = "dst_chain:";
        public const string sourceAssetLogPrefixPattern = "src_asset:";
        public const string amountInWeiLogPrefixPattern = "amount: ";
        public const string timelockLogPrefixPattern = "timelock: ";
        public const string receiverLogPrefixPattern = "src_receiver: ";
        public const string senderLogPrefixPattern = "sender: ";
        public const string hashlockLogPrefixPattern = "hashlock:";
    }
}