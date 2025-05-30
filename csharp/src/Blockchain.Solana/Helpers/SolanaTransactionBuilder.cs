﻿using System.Numerics;
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

        var managedAccount = network.ManagedAccounts.FirstOrDefault(x => x.Type == AccountType.Primary);

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
                Reward = Web3.Convert.ToWei(request.Reward, currency.Decimals),
                RewardTimelock = new BigInteger(request.RewardTimelock),
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

        var managedAccount = network.ManagedAccounts.FirstOrDefault(x => x.Type == AccountType.Primary);

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
                RewardPublicKey = request.DestinationAddress == managedAccount.Address?
                    new PublicKey(request.DestinationAddress) :
                    new PublicKey(request.SenderAddress),
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

        var managedAddress = network.ManagedAccounts.FirstOrDefault(x => x.Type == AccountType.Primary);

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

        var managedAccount = network.ManagedAccounts.FirstOrDefault(x => x.Type == AccountType.Primary);

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

        builder.SetAddLockSigInstruction(
            new PublicKey(htlcContractAddress),
            new HTLCAddlocksigRequest
            {
                AddLockSigMessageRequest = new()
                {
                    Id = request.Id.HexToByteArray(),
                    Hashlock = request.Hashlock.HexToByteArray(),
                    Timelock = request.Timelock,
                    SignerPublicKey = new PublicKey(request.SignerAddress),
                },
                Signature = Convert.FromBase64String(request.Signature!),
                SenderPublicKey = new PublicKey(managedAccount.Address),
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
}
