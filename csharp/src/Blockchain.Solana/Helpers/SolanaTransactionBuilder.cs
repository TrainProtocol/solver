using System.Numerics;
using System.Text.Json;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Solnet.Extensions;
using Solnet.Extensions.TokenMint;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Wallet;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram;
using Train.Solver.Blockchain.Solana.Programs.HTLCProgram.Models;
using Train.Solver.Blockchain.Solana.Models;
using System.Buffers.Binary;
using System.Text;
using System.Security.Cryptography;

namespace Train.Solver.Blockchain.Solana.Helpers;

public static class SolanaTransactionBuilder
{
    public static async Task<PrepareTransactionResponse> BuildHTLCLockTransactionAsync(Network network, string args)
    {
        var request = JsonSerializer.Deserialize<HTLCLockTransactionPrepareRequest>(args);

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.SourceAsset.ToUpper());

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency),
                $"Currency {request.SourceAsset} for {network.Name} is missing");
        }

        var managedAccount = network.ManagedAccounts.FirstOrDefault(x => x.Type == AccountType.LP);

        if (managedAccount == null)
        {
            throw new ArgumentNullException(nameof(managedAccount), $"Managed address for {network.Name} is not setup");
        }

        var nativeCurrency = network.Tokens.FirstOrDefault(x => x.IsNative);

        if (nativeCurrency == null)
        {
            throw new ArgumentNullException(nameof(nativeCurrency), $"Native currency for {network.Name} is not setup");
        }

        var node = network.Nodes.SingleOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node is not configured on {network.Name} network");
        }

        var htlcContractAddress = currency.IsNative
            ? network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        var rpcClient = ClientFactory.GetClient(node.Url);

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(managedAccount.Address));

        await GetOrCreateAssociatedTokenAccount(
            rpcClient,
            builder,
            currency,
            new PublicKey(managedAccount.Address),
            new PublicKey(managedAccount.Address));

        builder.SetLockTransactionInstruction(
            new PublicKey(htlcContractAddress),
            new HTLCLockRequest
            {
                Hashlock = request.Hashlock.HexToByteArray(),
                Id = request.Id.HexToByteArray(),
                SignerPublicKey = new PublicKey(managedAccount.Address),
                ReceiverPublicKey = new PublicKey(request.Receiver),
                Amount = Web3.Convert.ToWei(request.Amount, currency.Decimals),
                Timelock = new BigInteger(request.Timelock),
                SourceAsset = currency.Asset,
                DestinationNetwork = request.DestinationNetwork,
                SourceAddress = request.DestinationAddress,
                DestinationAsset = request.DestinationAsset,
                SourceTokenPublicKey = new PublicKey(currency.TokenContract),
            });

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var serializedTx = Convert.ToBase64String(builder.Serialize());
        var response = new PrepareTransactionResponse
        {
            Data = serializedTx,
            ToAddress = htlcContractAddress,
            Amount = 0,
            Asset = nativeCurrency.Asset,
            AmountInWei = "0",
            CallDataAmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString(),
            CallDataAmount = request.Amount,
            CallDataAsset = currency.Asset,
        };

        if (nativeCurrency.Id == currency.Id)
        {
            response.Amount = request.Amount;
            response.AmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString();
        }

        return response;
    }

    public static async Task<PrepareTransactionResponse> BuildHTLCRedeemTransactionAsync(Network network, string args)
    {
        var request = JsonSerializer.Deserialize<HTLCRedeemTransactionPrepareRequest>(args);

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

        var managedAccount = network.ManagedAccounts.FirstOrDefault(x => x.Type == AccountType.LP);

        if (managedAccount == null)
        {
            throw new ArgumentNullException(nameof(managedAccount), $"Managed address for {network.Name} is not setup");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency),
                $"Currency {request.Asset} for {network.Name} is missing");
        }

        var nativeCurrency = network.Tokens.FirstOrDefault(x => x.TokenContract is null);

        if (nativeCurrency == null)
        {
            throw new ArgumentNullException(nameof(nativeCurrency), $"Native currency for {network.Name} is not setup");
        }

        var node = network.Nodes.SingleOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node is not configured on {network.Name} network");
        }

        var htlcContractAddress = currency.IsNative
            ? network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        var rpcClient = ClientFactory.GetClient(node.Url);

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(managedAccount.Address));

        await GetOrCreateAssociatedTokenAccount(
            rpcClient,
            builder,
            currency,
            new PublicKey(request.DestinationAddress),
            new PublicKey(managedAccount.Address));

        builder.SetRedeemTransactionInstruction(
            new PublicKey(htlcContractAddress),
            new HTLCRedeemRequest
            {
                Id = request.Id.HexToByteArray(),
                Secret = BigInteger.Parse(request.Secret).ToHexBigInteger().HexValue.HexToByteArray(),
                SourceTokenPublicKey = new PublicKey(currency.TokenContract),
                SignerPublicKey = new PublicKey(managedAccount.Address),
                ReceiverPublicKey = new PublicKey(request.DestinationAddress),
                SenderPublicKey = new PublicKey(request.SenderAddress),
            });

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var serializedTx = Convert.ToBase64String(builder.Serialize());
        var response = new PrepareTransactionResponse
        {
            Data = serializedTx,
            ToAddress = htlcContractAddress,
            Amount = 0,
            Asset = nativeCurrency.Asset,
            AmountInWei = "0",
            CallDataAsset = currency.Asset,
            CallDataAmountInWei = "0",
            CallDataAmount = 0,
        };

        return response;
    }

    public static async Task<PrepareTransactionResponse> BuildHTLCRefundTransactionAsync(Network network, string args)
    {
        var request = JsonSerializer.Deserialize<HTLCRefundTransactionPrepareRequest>(args);

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        if (string.IsNullOrEmpty(request.DestinationAddress))
        {
            throw new ArgumentNullException(nameof(request.DestinationAddress), "Receiver address is required");
        }

        var managedAddress = network.ManagedAccounts.FirstOrDefault(x => x.Type == AccountType.LP);

        if (managedAddress == null)
        {
            throw new ArgumentNullException(nameof(managedAddress), $"Managed address for {network.Name} is not setup");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency), "Currency {request.Asset} for {network.Name} is missing");
        }

        var nativeCurrency = network.Tokens.FirstOrDefault(x => x.TokenContract is null);

        if (nativeCurrency == null)
        {
            throw new ArgumentNullException(nameof(nativeCurrency), $"Native currency for {network.Name} is not setup");
        }

        var node = network.Nodes.SingleOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node is not configured on {network.Name} network");
        }

        var htlcContractAddress = currency.IsNative
            ? network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        var rpcClient = ClientFactory.GetClient(node.Url);

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(managedAddress.Address));

        await GetOrCreateAssociatedTokenAccount(
            rpcClient,
            builder,
            currency,
            new PublicKey(request.DestinationAddress),
            new PublicKey(managedAddress.Address));

        builder.SetRefundTransactionInstruction(
            new PublicKey(htlcContractAddress),
            new HTLCRefundRequest
            {
                Id = request.Id.HexToByteArray(),
                SourceTokenPublicKey = new PublicKey(currency.TokenContract),
                SignerPublicKey = new PublicKey(managedAddress.Address),
                ReceiverPublicKey = new PublicKey(request.DestinationAddress)
            });

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var serializedTx = Convert.ToBase64String(builder.Serialize());
        var response = new PrepareTransactionResponse
        {
            Data = serializedTx,
            ToAddress = htlcContractAddress,
            Amount = 0,
            Asset = nativeCurrency.Asset,
            AmountInWei = "0",
            CallDataAsset = currency.Asset,
            CallDataAmountInWei = "0",
            CallDataAmount = 0,
        };

        return response;
    }

    public static async Task<PrepareTransactionResponse> BuildTransferTransactionAsync(Network network, string args)
    {

        var request = JsonSerializer.Deserialize<TransferPrepareRequest>(args);

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var node = network.Nodes.SingleOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node is not configured on {network.Name} network");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset == request.Asset);

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

        if (request.Memo != null)
        {
            builder.AddInstruction(MemoProgram.NewMemo(publicKeyFromAddress, request.Memo));
        }

        var serializedTx = Convert.ToBase64String(builder.Serialize());

        var response = new PrepareTransactionResponse
        {
            Data = serializedTx,
            ToAddress = request.ToAddress,
            Amount = request.Amount,
            Asset = request.Asset,
            AmountInWei = amountInBaseUnits.ToString(),
            CallDataAmount = request.Amount,
            CallDataAmountInWei = amountInBaseUnits.ToString(),
            CallDataAsset = currency.Asset,
        };

        return response;
    }

    public static async Task<PrepareTransactionResponse> BuildHTLCAddlockSigTransactionAsync(Network network, string args)
    {
        var request = JsonSerializer.Deserialize<AddLockSigTransactionPrepareRequest>(args);

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

        var managedAccount = network.ManagedAccounts.FirstOrDefault(x => x.Type == AccountType.LP);

        if (managedAccount == null)
        {
            throw new ArgumentNullException(nameof(managedAccount), $"Managed address for {network.Name} is not setup");
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            throw new ArgumentNullException(nameof(currency),
                $"Currency {request.Asset} for {network.Name} is missing");
        }

        var nativeCurrency = network.Tokens.FirstOrDefault(x => x.TokenContract is null);

        if (nativeCurrency == null)
        {
            throw new ArgumentNullException(nameof(nativeCurrency), $"Native currency for {network.Name} is not setup");
        }

        var node = network.Nodes.SingleOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), $"Node is not configured on {network.Name} network");
        }

        var htlcContractAddress = currency.IsNative
            ? network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        var rpcClient = ClientFactory.GetClient(node.Url);

        var builder = new TransactionBuilder()
            .SetFeePayer(new PublicKey(managedAccount.Address));

        var addLockSigMessage = CreateAddLockSigMessage(new SolanaAddLockSigMessage
        {
            Id = request.Id,
            Hashlock = request.Hashlock,
            Timelock = request.Timelock,
            SignerAddress = request.SignerAddress,
        });

        builder.SetAddLockSigInstruction(
            new PublicKey(htlcContractAddress),
            new HTLCAddlocksigRequest
            {
                Id = request.Id.HexToByteArray(),
                Hashlock = request.Hashlock.HexToByteArray(),
                Timelock = new BigInteger(request.Timelock),
                Signature = Convert.FromBase64String(request.Signature!),
                Message = addLockSigMessage,
                SenderPublicKey = new PublicKey(managedAccount.Address),
                SignerPublicKey = new PublicKey(request.SignerAddress),
            });

        var latestBlockHashResponse = await rpcClient.GetLatestBlockHashAsync();

        if (!latestBlockHashResponse.WasSuccessful)
        {
            throw new Exception($"Failed to get latest block hash, error: {latestBlockHashResponse.RawRpcResponse}");
        }

        builder.SetRecentBlockHash(latestBlockHashResponse.Result.Value.Blockhash);

        var serializedTx = Convert.ToBase64String(builder.Serialize());
        var response = new PrepareTransactionResponse
        {
            Data = serializedTx,
            ToAddress = htlcContractAddress,
            Amount = 0,
            Asset = nativeCurrency.Asset,
            AmountInWei = "0",
            CallDataAsset = currency.Asset,
            CallDataAmountInWei = "0",
            CallDataAmount = 0,
        };

        return response;
    }

    public static async Task<TransactionBuilder> CreateTransactionInstructionAsync(
        this TransactionBuilder builder,
        Token currency,
        IRpcClient rpcClient,
        PublicKey publicKeyFromAddress,
        string toAddress,
        ulong amountInWei,
        bool createAssociatedTokenAccount = true)
    {
        var publicKeyToAddress = new PublicKey(toAddress);

        if (string.IsNullOrEmpty(currency.TokenContract))
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
                    currency.TokenContract,
                    currency.Asset,
                    currency.Asset,
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
                        currency.TokenContract,
                        publicKeyFromAddress);
                }
                else
                {
                    destination = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(publicKeyToAddress,
                        new PublicKey(currency.TokenContract));
                }

                var source = sourceWallet.JitCreateAssociatedTokenAccount(
                    builder,
                    currency.TokenContract,
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

    public async static Task GetOrCreateAssociatedTokenAccount(
        IRpcClient rpcClient,
        TransactionBuilder builder,
        Token currency,
        PublicKey ownerPublicKey,
        PublicKey feePayerPublicKey)
    {
        try
        {
            var token = new TokenDef(
                currency.TokenContract,
                currency.Asset,
                currency.Asset,
                currency.Decimals);

            var tokenDefs = new TokenMintResolver();
            tokenDefs.Add(token);

            var wallet = await TokenWallet.LoadAsync(rpcClient, tokenDefs, ownerPublicKey);

            wallet.JitCreateAssociatedTokenAccount(
                builder,
                currency.TokenContract,
                feePayerPublicKey);
        }
        catch (TokenWalletException ex)
        {
            throw new Exception("Failed to load token wallet", ex);
        }
    }

    public static byte[] CreateAddLockSigMessage(SolanaAddLockSigMessage messageRequest)
    {
        var idBytes = Convert.FromHexString(messageRequest.Id);
        var hashlockBytes = Convert.FromHexString(messageRequest.Hashlock);

        var timelockLe = new byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(timelockLe, (ulong)messageRequest.Timelock);

        byte[] msg;
        using (var sha = SHA256.Create())
        {
            sha.TransformBlock(idBytes, 0, idBytes.Length, null, 0);
            sha.TransformBlock(hashlockBytes, 0, hashlockBytes.Length, null, 0);
            sha.TransformFinalBlock(timelockLe, 0, timelockLe.Length);
            msg = sha.Hash!;
        }

        var signingDomain = new byte[] { 0xFF }
            .Concat(Encoding.ASCII.GetBytes("solana offchain"))
            .ToArray();

        var headerVersion = new byte[] { 0x00 };
        var applicationDomain = new byte[32];
        Encoding.ASCII.GetBytes("Train", applicationDomain);
        var messageFormat = new byte[] { 0x00 };
        var signerCount = new byte[] { 0x01 };

        var signerPublicKey = new PublicKey(messageRequest.SignerAddress).KeyBytes;

        var messageLengthLe = BitConverter.GetBytes((ushort)msg.Length);

        var parts = new List<byte[]>
        {
            signingDomain,
            headerVersion,
            applicationDomain,
            messageFormat,
            signerCount,
            signerPublicKey,
            messageLengthLe,
            msg
        };

        var totalLength = 0;
        foreach (var p in parts) totalLength += p.Length;

        var finalMessage = new byte[totalLength];
        var offset = 0;
        foreach (var p in parts)
        {
            Buffer.BlockCopy(p, 0, finalMessage, offset, p.Length);
            offset += p.Length;
        }

        return finalMessage;
    }
}
