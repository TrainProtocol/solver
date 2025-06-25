using System.Numerics;
using System.Text.Json;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using Train.Solver.Blockchain.EVM.FunctionMessages;
using Train.Solver.Data.Abstractions.Entities;
using Train.Solver.Blockchain.Abstractions.Models;
using Train.Solver.Util.Extensions;

namespace Train.Solver.Blockchain.EVM.Helpers;

public static class EVMTransactionBuilder
{
    public static PrepareTransactionResponse BuildApproveTransaction(Network network, string args)
    {
        var request = JsonSerializer.Deserialize<ApprovePrepareRequest>(args);

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.Single(x => x.Asset == request.Asset);

        var nativeCurrency = network.Tokens.Single(x => string.IsNullOrEmpty(x.TokenContract));

        var spenderAddress = string.IsNullOrEmpty(currency.TokenContract) ?
           network.HTLCTokenContractAddress
           : network.HTLCTokenContractAddress;

        if (currency.Id != nativeCurrency.Id)
        {
            var response = new PrepareTransactionResponse
            {
                ToAddress = currency.TokenContract!,
                AmountInWei = BigInteger.Zero.ToString(),
                Asset = nativeCurrency.Asset,
                Data = new FunctionMessages.ApproveFunction
                {
                    Spender = spenderAddress,
                    Value = Web3.Convert.ToWei(request.Amount, currency.Decimals)
                }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),
                CallDataAsset = nativeCurrency.Asset,
                CallDataAmountInWei = BigInteger.Zero.ToString(),
            };

            return response;
        }

        throw new Exception("Requested currency does not have contract address");
    }

    public static PrepareTransactionResponse BuildTransferTransaction(
        Network network,
        string args)
    {
        var request = JsonSerializer.Deserialize<TransferPrepareRequest>(args);

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var response = new PrepareTransactionResponse();
        string memoHex = "";

        if (!string.IsNullOrEmpty(request.Memo))
        {
            memoHex = BigInteger.Parse(request.Memo).ToHexBigInteger().HexValue;
            response.Data = memoHex;
        }

        var currency = network.Tokens.Single(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        var nativeCurrency = network.Tokens.Single(x => string.IsNullOrEmpty(x.TokenContract));
        response.Asset = nativeCurrency.Asset;

        if (currency.Id != nativeCurrency.Id)
        {
            response.Data = $"{new TransferFunction
            {
                To = request.ToAddress,
                Value = Web3.Convert.ToWei(request.Amount, currency.Decimals),
            }.GetCallData().ToHex().EnsureEvenLengthHex()}{memoHex.RemoveHexPrefix()}".EnsureHexPrefix();

            response.AmountInWei = BigInteger.Zero.ToString();
            response.ToAddress = currency.TokenContract!;
            response.CallDataAsset = currency.Asset;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString();
        }
        else
        {
            response.AmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString();
            response.ToAddress = request.ToAddress;
            response.CallDataAsset = currency.Asset;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString();
        }

        return response;
    }

    public static PrepareTransactionResponse BuildHTLCAddLockSigTransaction(Network network, string args)
    {
        var request = JsonSerializer.Deserialize<AddLockSigTransactionPrepareRequest>(args);

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.Single(x => x.Asset.ToUpper() == request.Asset.ToUpper());
        var isNative = currency.Id == network.NativeTokenId;

        var htlcContractAddress = isNative
            ? network.HTLCTokenContractAddress
            : network.HTLCTokenContractAddress;

        var nativeCurrency = network.Tokens.First(x => x.TokenContract is null);

        var hashlock = request.Hashlock.ToBytes32();

        var response = new PrepareTransactionResponse
        {
            Data = new AddLockSigFunction
            {
                Message = new AddLockMessage
                {
                    Id = request.Id.ToBytes32(),
                    Hashlock = hashlock,
                    Timelock = (ulong)request.Timelock
                },
                R = request.R!.ToBytes32(),
                S = request.S!.ToBytes32(),
                V = byte.Parse(request.V!)
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),

            AmountInWei = BigInteger.Zero.ToString(),
            Asset = nativeCurrency.Asset,
            ToAddress = htlcContractAddress,
            CallDataAsset = request.Asset,
            CallDataAmountInWei = BigInteger.Zero.ToString()
        };

        return response;
    }

    public static PrepareTransactionResponse BuildHTLCCommitTransaction(Network network, string args)
    {
        var request = JsonSerializer.Deserialize<HTLCCommitTransactionPrepareRequest>(args);

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.Single(x => x.Asset.ToUpper() == request.SourceAsset.ToUpper());

        var nativeCurrency = network.Tokens.First(x => x.TokenContract is null);

        var response = new PrepareTransactionResponse();

        //ERC20 HTLC call
        if (currency.Id != nativeCurrency.Id)
        {
            response.ToAddress = network.HTLCTokenContractAddress;

            response.Data = new ERC20CommitFunction
            {
                Chains = request.HopChains,
                DestinationAddresses = request.HopAddresses,
                DestinationChain = request.DestinationChain,
                DestinationAsset = request.DestinationAsset,
                DestinationAddress = request.DestinationAddress,
                SourceAsset = request.SourceAsset,
                Receiver = request.Receiever,
                Timelock = request.Timelock,
                Amount = Web3.Convert.ToWei(request.Amount, currency.Decimals),
                TokenContract = currency.TokenContract
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.AmountInWei = BigInteger.Zero.ToString();
            response.Asset = nativeCurrency.Asset;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString();
        }
        else
        {
            response.ToAddress = network.HTLCTokenContractAddress;

            response.Data = new CommitFunction
            {
                HopChains = request.HopChains,
                HopAssets = request.HopAssets,
                HopAddresses = request.HopAddresses,
                DestinationChain = request.DestinationChain,
                DestinationAsset = request.DestinationAsset,
                DestinationAddress = request.DestinationAddress,
                SourceAsset = request.SourceAsset,
                Receiver = request.Receiever,
                Timelock = request.Timelock,
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.AmountInWei = Web3.Convert.ToWei(request.Amount, nativeCurrency.Decimals).ToString();
            response.Asset = nativeCurrency.Asset;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount, nativeCurrency.Decimals).ToString();
        }

        return response;

    }

    public static PrepareTransactionResponse BuildHTLCLockTransaction(
        Network network,
        string args)
    {
        var request = JsonSerializer.Deserialize<HTLCLockTransactionPrepareRequest>(args);

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var response = new PrepareTransactionResponse();

        var currency = network.Tokens.Single(x => x.Asset.ToUpper() == request.SourceAsset.ToUpper());
        var isNative = currency.Id == network.NativeTokenId;

        var nativeCurrency = network.Tokens.First(x => x.TokenContract is null);

        var hashlock = request.Hashlock.ToBytes32();

        response.Asset = nativeCurrency.Asset;

        if (!isNative)
        {
            response.Data = new ERC20LockFunction
            {
                LockParams = new ERC20LockMessage
                {
                    Id = request.Id.ToBytes32(),
                    SourceReceiver = request.Receiver,
                    Hashlock = hashlock,
                    Timelock = request.Timelock,
                    SourceAsset = request.SourceAsset,
                    DestinationChain = request.DestinationNetwork,
                    DestinationAddress = request.DestinationAddress,
                    DestinationAsset = request.DestinationAsset,
                    Reward = BigInteger.Parse(request.Reward),
                    RewardTimelock = request.RewardTimelock,
                    Amount = BigInteger.Parse(request.Amount),
                    TokenContract = currency.TokenContract!,
                }
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.AmountInWei = "0";
            response.Asset = nativeCurrency.Asset;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmountInWei = (BigInteger.Parse(request.Amount) + BigInteger.Parse(request.Reward)).ToString();
        }
        else
        {
            response.Data = new LockFunction
            {
                Id = request.Id.ToBytes32(),
                Hashlock = hashlock,
                Timelock = request.Timelock,
                SourceReceiver = request.Receiver,
                SourceAsset = request.SourceAsset,
                DestinationChain = request.DestinationNetwork,
                DestinationAddress = request.DestinationAddress,
                DestinationAsset = request.DestinationAsset,
                Reward = BigInteger.Parse(request.Reward),
                RewardTimelock = request.RewardTimelock,
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.AmountInWei = request.Amount;
            response.Asset = nativeCurrency.Asset;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmountInWei = (BigInteger.Parse(request.Amount) + BigInteger.Parse(request.Reward)).ToString();
        }

        var htlcContractAddress = isNative
            ? network.HTLCTokenContractAddress
            : network.HTLCTokenContractAddress;

        response.ToAddress = htlcContractAddress;

        return response;
    }

    public static PrepareTransactionResponse BuildHTLCRedeemTranaction(
        Network network,
        string args)
    {
        var request = JsonSerializer.Deserialize<HTLCRedeemTransactionPrepareRequest>(args);

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.Single(x => x.Asset.ToUpper() == request.Asset.ToUpper());
        var isNative = currency.Id == network.NativeTokenId;

        var lockId = request.Id.ToBytes32();
        var secret = BigInteger.Parse(request.Secret);

        var htlcContractAddress = isNative
            ? network.HTLCTokenContractAddress
            : network.HTLCTokenContractAddress;

        return new PrepareTransactionResponse
        {
            Data = new RedeemFunction
            {
                Id = lockId,
                Secret = secret
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),
            AmountInWei = "0",
            ToAddress = htlcContractAddress,
            Asset = network.NativeToken.Asset,
            CallDataAsset = request.Asset,
            CallDataAmountInWei = "0",
        };
    }

    public static PrepareTransactionResponse BuildHTLCRefundTransaction(Network network, string args)
    {
        var request = JsonSerializer.Deserialize<HTLCRefundTransactionPrepareRequest>(args);

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.Single(x => x.Asset.ToUpper() == request.Asset.ToUpper());
        var isNative = currency.Id == network.NativeTokenId;

        var htlcId = request.Id.ToBytes32();

        var htlcContractAddress = isNative
            ? network.HTLCTokenContractAddress
            : network.HTLCTokenContractAddress;

        return new PrepareTransactionResponse
        {
            Data = new RefundFunction
            {
                Id = htlcId,
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),
            AmountInWei = "0",
            ToAddress = htlcContractAddress,
            Asset = network.NativeToken.Asset,
            CallDataAsset = request.Asset,
            CallDataAmountInWei = "0",
        };
    }
}
