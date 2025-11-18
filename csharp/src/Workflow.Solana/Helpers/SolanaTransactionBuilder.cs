using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using System.Numerics;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;
using Train.Solver.Common.Extensions;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.Solana.Helpers;

public static class SolanaTransactionBuilder
{
    public static async Task<PrepareTransactionDto> BuildHTLCLockTransactionAsync(
        DetailedNetworkDto network,
        string solverAccount,
        string args)
    {
        var request = args.FromJson<HTLCLockTransactionPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Symbol.ToUpper() == request.SourceAsset.ToUpper());

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency),
                $"Currency {request.SourceAsset} for {network.Name} is missing");
        }

        var account = new Account();

        var isNative = currency.Symbol.ToUpper() == network.NativeToken!.Symbol.ToUpper();

        var node = network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new($"Node is not configured on {network.Name} network");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var htlcContractAddress = isNative
            ? network.HTLCNativeContractAddress
            : network.HTLCTokenContractAddress;

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(solverAccount));

        await GetOrCreateAssociatedTokenAccount(
            rpcClient,
            builder,
            currency,
            new PublicKey(solverAccount),
            new PublicKey(solverAccount));

        builder.SetLockTransactionInstruction(
            new PublicKey(htlcContractAddress),
            new HTLCLockRequest
            {
                Hashlock = request.Hashlock.HexToByteArray(),
                Id = request.CommitId.HexToByteArray(),
                SignerPublicKey = new PublicKey(solverAccount),
                ReceiverPublicKey = new PublicKey(request.Receiver),
                Amount = request.Amount,
                Timelock = new BigInteger(request.Timelock),
                SourceAsset = currency.Symbol,
                DestinationNetwork = request.DestinationNetwork,
                SourceAddress = request.DestinationAddress,
                DestinationAsset = request.DestinationAsset,
                SourceTokenPublicKey = new PublicKey(currency.Contract),
                Reward = request.Reward,
                RewardTimelock = new BigInteger(request.RewardTimelock),
            });

        var latestBlockResult = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockResult.WasSuccessful)
        {
            throw new ($"Failed to get last valid block");
        }

        builder.SetRecentBlockHash(latestBlockResult.Result.Value.Blockhash);

        var serializedTx = Convert.ToBase64String(builder.Serialize());

        var response = new PrepareTransactionDto
        {
            Data = serializedTx,
            ToAddress = htlcContractAddress,
            Asset = network.NativeToken.Symbol,
            Amount = 0
        };

        if (isNative)
        {
            response.Amount = request.Amount;
        }

        return response;
    }

    public static async Task<PrepareTransactionDto> BuildHTLCRedeemTransactionAsync(
        DetailedNetworkDto network,
        string solverAccount,
        string args)
    {
        var request = args.FromJson<HTLCRedeemTransactionPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        if (string.IsNullOrEmpty(request.DestinationAddress))
        {
            throw new ArgumentNullException(nameof(request.DestinationAddress), "Receiver address is required");
        }

        if (string.IsNullOrEmpty(request.SenderAddress))
        {
            throw new ArgumentNullException(nameof(request.SenderAddress), "Sender address is required");
        }
       
        var currency = network.Tokens.SingleOrDefault(x => x.Symbol.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency),
                $"Currency {request.Asset} for {network.Name} is missing");
        }

        var isNative = currency.Symbol.ToUpper() == network.NativeToken!.Symbol.ToUpper();

        var node = network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new($"Node is not configured on {network.Name} network");
        }

        var rpcClient = ClientFactory.GetClient(node.Url);

        var htlcContractAddress = isNative
            ? network.HTLCNativeContractAddress
            : network.HTLCTokenContractAddress;

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(solverAccount));

         await GetOrCreateAssociatedTokenAccount(
            rpcClient,
            builder,
            currency,
            new PublicKey(solverAccount),
            new PublicKey(solverAccount));

        builder.SetRedeemTransactionInstruction(
            new PublicKey(htlcContractAddress),
            new HTLCRedeemRequest
            {
                Id = request.CommitId.HexToByteArray(),
                Secret = BigInteger.Parse(request.Secret).ToHexBigInteger().HexValue.HexToByteArray(),
                SourceTokenPublicKey = new PublicKey(currency.Contract),
                SignerPublicKey = new PublicKey(solverAccount),
                ReceiverPublicKey = new PublicKey(request.DestinationAddress),
                SenderPublicKey = new PublicKey(request.SenderAddress),
                RewardPublicKey = request.DestinationAddress == solverAccount?
                    new PublicKey(request.DestinationAddress) :
                    new PublicKey(request.SenderAddress),
            });

        var latestBlockResult = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockResult.WasSuccessful)
        {
            throw new($"Failed to get last valid block");
        }

        builder.SetRecentBlockHash(latestBlockResult.Result.Value.Blockhash);

        var serializedTx = Convert.ToBase64String(builder.Serialize());
        var response = new PrepareTransactionDto
        {
            Data = serializedTx,
            ToAddress = htlcContractAddress,
            Asset = network.NativeToken.Symbol,
            Amount = 0,
        };

        return response;
    }

    public static async Task<PrepareTransactionDto> BuildHTLCRefundTransactionAsync(
        DetailedNetworkDto network,
        string solverAccount,
        string args)
    {
        var request = args.FromJson<HTLCRefundTransactionPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        if (string.IsNullOrEmpty(request.DestinationAddress))
        {
            throw new ArgumentNullException(nameof(request.DestinationAddress), "Receiver address is required");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Symbol.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency), "Currency {request.Asset} for {network.Name} is missing");
        }

        var isNative = currency.Symbol.ToUpper() == network.NativeToken!.Symbol.ToUpper();

        var node = network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node is not configured on {network.Name} network");
        }

        var htlcContractAddress = isNative
            ? network.HTLCNativeContractAddress
            : network.HTLCTokenContractAddress;

        var rpcClient = ClientFactory.GetClient(node.Url);

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(solverAccount));

        await GetOrCreateAssociatedTokenAccount(
            rpcClient,
            builder,
            currency,
            new PublicKey(request.DestinationAddress),
            new PublicKey(solverAccount));

        builder.SetRefundTransactionInstruction(
            new PublicKey(htlcContractAddress),
            new HTLCRefundRequest
            {
                Id = request.CommitId.HexToByteArray(),
                SourceTokenPublicKey = new PublicKey(currency.Contract),
                SignerPublicKey = new PublicKey(solverAccount),
                ReceiverPublicKey = new PublicKey(request.DestinationAddress)
            });

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var serializedTx = Convert.ToBase64String(builder.Serialize());
        var response = new PrepareTransactionDto
        {
            Data = serializedTx,
            ToAddress = htlcContractAddress,
            Asset = network.NativeToken.Symbol,
            Amount = 0,
        };

        return response;
    }

    public static async Task<PrepareTransactionDto> BuildTransferTransactionAsync(
        DetailedNetworkDto network,
        string args)
    {
        var request = args.FromJson<TransferPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var node = network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node is not configured on {network.Name} network");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Symbol == request.Asset);

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency),
                $"Currency {request.Asset} for {network.Name} is missing");
        }

        if (request.FromAddress is null)
        {
            throw new ArgumentNullException(nameof(request.FromAddress), "From address is required");
        }

        var publicKeyFromAddress = new PublicKey(request.FromAddress);
        var amountInBaseUnits = (ulong)Web3.Convert.ToWei(request.Amount, currency.Decimals);
        var rpcClient = ClientFactory.GetClient(node.Url);

        var builder = new TransactionBuilder()
            .SetFeePayer(publicKeyFromAddress);

        var transactionInstructionResult = await builder.CreateTransactionInstructionAsync(
            currency,
            rpcClient,
            publicKeyFromAddress,
            request.ToAddress,
            amountInBaseUnits);

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var serializedTx = Convert.ToBase64String(builder.Serialize());

        var response = new PrepareTransactionDto
        {
            Data = serializedTx,
            ToAddress = request.ToAddress,
            Asset = request.Asset,
            Amount = request.Amount,
        };

        return response;
    }

    public static async Task<PrepareTransactionDto> BuildHTLCAddlockSigTransactionAsync(
        DetailedNetworkDto network,
        string solverAccount,
        string args)
    {
        var request = args.FromJson<AddLockSigTransactionPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        if (string.IsNullOrEmpty(request.Signature))
        {
            throw new ArgumentNullException(nameof(request.Signature), "Signature is required");
        }

        if (string.IsNullOrEmpty(request.SignerAddress))
        {
            throw new ArgumentNullException(nameof(request.SignerAddress), "Sender address is required");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Symbol.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency),
                $"Currency {request.Asset} for {network.Name} is missing");
        }

        var isNative = currency.Symbol.ToUpper() == network.NativeToken!.Symbol.ToUpper();
        var node = network.Nodes.FirstOrDefault();

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node is not configured on {network.Name} network");
        }

        var htlcContractAddress = isNative
            ? network.HTLCNativeContractAddress
            : network.HTLCTokenContractAddress;

        var rpcClient = ClientFactory.GetClient(node.Url);

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(solverAccount));

        builder.SetAddLockSigInstruction(
            new PublicKey(htlcContractAddress),
            new HTLCAddlocksigRequest
            {
                AddLockSigMessageRequest = new()
                {
                    Id = request.CommitId.HexToByteArray(),
                    Hashlock = request.Hashlock.HexToByteArray(),
                    Timelock = request.Timelock,
                    SignerPublicKey = new PublicKey(request.SignerAddress),
                },
                Signature = Convert.FromBase64String(request.Signature!),
                SenderPublicKey = new PublicKey(solverAccount),
            });

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var serializedTx = Convert.ToBase64String(builder.Serialize());
        var response = new PrepareTransactionDto
        {
            Data = serializedTx,
            ToAddress = htlcContractAddress,
            Asset = network.NativeToken.Symbol,
            Amount = 0,
        };

        return response;
    }

    public static async Task<TransactionBuilder> CreateTransactionInstructionAsync(
        this TransactionBuilder builder,
        TokenDto currency,
        IRpcClient rpcClient,
        PublicKey publicKeyFromAddress,
        string toAddress,
        ulong amountInWei,
        bool createAssociatedTokenAccount = true)
    {
        var publicKeyToAddress = new PublicKey(toAddress);

        if (string.IsNullOrEmpty(currency.Contract))
        {
            //SolanaTransactionProcessorWorkflow transfer
            builder.AddInstruction(
                SystemProgram.Transfer(
                    fromPublicKey: publicKeyFromAddress,
                    toPublicKey: publicKeyToAddress,
                    amountInWei));
        }
        else
        {
            try
            {
                //SPL token transfer
                var token = new TokenDef(
                    currency.Contract,
                    currency.Symbol,
                    currency.Symbol,
                    currency.Decimals);

                var tokenDefs = new TokenMintResolver();
                tokenDefs.Add(token);

                var sourceWallet = await TokenWallet.LoadAsync(rpcClient, tokenDefs, publicKeyFromAddress);

                var destinationWallet = await TokenWallet.LoadAsync(rpcClient, tokenDefs, publicKeyToAddress);

                PublicKey destination;
                if (createAssociatedTokenAccount)
                {
                    destination = destinationWallet.JitCreateAssociatedTokenAccount(
                        builder,
                        currency.Contract,
                        publicKeyFromAddress);
                }
                else
                {
                    destination = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(publicKeyToAddress,
                        new PublicKey(currency.Contract));
                }

                var source = sourceWallet.JitCreateAssociatedTokenAccount(
                    builder,
                    currency.Contract,
                    publicKeyFromAddress);

                builder.AddInstruction(
                    TokenProgram.Transfer(
                        source,
                        destination,
                        amountInWei,
                        publicKeyFromAddress));
            }
            catch (TokenWalletException ex)
            {
                throw new Exception($"Fail to load token wallet", ex);
            }
        }

        return builder;
    }

    private static async Task GetOrCreateAssociatedTokenAccount(
        IRpcClient rpcClient,
        TransactionBuilder builder,
        TokenDto currency,
        PublicKey ownerPublicKey,
        PublicKey feePayerPublicKey)
    {
        try
        {
            var token = new TokenDef(
                currency.Contract,
                currency.Symbol,
                currency.Symbol,
                currency.Decimals);

            var tokenDefs = new TokenMintResolver();
            tokenDefs.Add(token);

            var wallet = await TokenWallet.LoadAsync(rpcClient, tokenDefs, ownerPublicKey);

            wallet.JitCreateAssociatedTokenAccount(
                builder,
                currency.Contract,
                feePayerPublicKey);
        }
        catch (TokenWalletException ex)
        {
            throw new Exception("Failed to load token wallet", ex);
        }
    }    
}
