namespace Train.Solver.Blockchain.Solana.Helpers;

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

    public const string memoType = "spl-memo";

    public const string LockSighash = "global:lock";
    public const string LockRewardSighash = "global:lock_reward";
    public const string RefundSighash = "global:refund";
    public const string RedeemSighash = "global:redeem";
    public const string GetDetailsSighash = "global:getDetails";
    public const string AddLockSigSighash = "global:add_lock_sig";
    public const string SysVarInstructionAddress = "Sysvar1nstructions1111111111111111111111111";

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
