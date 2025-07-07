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
           network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
           : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        if (currency.Id != nativeCurrency.Id)
        {
            var response = new PrepareTransactionResponse
            {
                ToAddress = currency.TokenContract,
                Amount = 0,
                AmountInWei = "0",
                Asset = nativeCurrency.Asset,
                Data = new FunctionMessages.ApproveFunction
                {
                    Spender = spenderAddress,
                    Value = Web3.Convert.ToWei(request.Amount, currency.Decimals)
                }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),
                CallDataAsset = nativeCurrency.Asset,
                CallDataAmountInWei = "0",
                CallDataAmount = 0
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

            response.Amount = 0m;
            response.AmountInWei = "0";
            response.ToAddress = currency.TokenContract;
            response.CallDataAsset = currency.Asset;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString();
            response.CallDataAmount = request.Amount;
        }
        else
        {
            response.Amount = request.Amount;
            response.AmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString();
            response.ToAddress = request.ToAddress;
            response.CallDataAsset = currency.Asset;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString();
            response.CallDataAmount = request.Amount;
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

        var htlcContractAddress = currency.IsNative
            ? network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

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

            Amount = 0,
            AmountInWei = "0",
            Asset = nativeCurrency.Asset,
            ToAddress = htlcContractAddress,
            CallDataAsset = request.Asset,
            CallDataAmount = 0,
            CallDataAmountInWei = "0"
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
            response.ToAddress = network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

            response.Data = new ERC20CommitFunction
            {
                Id = request.Id.ToBytes32(),
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

            response.Amount = 0;
            response.AmountInWei = "0";
            response.Asset = nativeCurrency.Asset;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmount = request.Amount;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString();
        }
        else
        {
            response.ToAddress = network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address;

            response.Data = new CommitFunction
            {
                Id = request.Id.ToBytes32(),
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

            response.Amount = request.Amount;
            response.AmountInWei = Web3.Convert.ToWei(request.Amount, nativeCurrency.Decimals).ToString();
            response.Asset = nativeCurrency.Asset;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmount = request.Amount;
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

        var nativeCurrency = network.Tokens.First(x => x.TokenContract is null);

        var hashlock = request.Hashlock.ToBytes32();

        response.Asset = nativeCurrency.Asset;

        if (currency.Id != nativeCurrency.Id)
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
                    Reward = Web3.Convert.ToWei(request.Reward, currency.Decimals),
                    RewardTimelock = request.RewardTimelock,
                    Amount = Web3.Convert.ToWei(request.Amount, currency.Decimals),
                    TokenContract = currency.TokenContract!,
                }
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.Amount = 0;
            response.AmountInWei = "0";
            response.Asset = nativeCurrency.Asset;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmount = request.Amount + request.Reward;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount + request.Reward, currency.Decimals).ToString();
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
                Reward = Web3.Convert.ToWei(request.Reward, currency.Decimals),
                RewardTimelock = request.RewardTimelock,
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.Amount = request.Amount;
            response.AmountInWei = Web3.Convert.ToWei(request.Amount, nativeCurrency.Decimals).ToString();
            response.Asset = nativeCurrency.Asset;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmount = request.Amount + request.Reward;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount + request.Reward, nativeCurrency.Decimals).ToString();
        }

        var htlcContractAddress = currency.IsNative
            ? network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

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

        var nativeCurrency = network.Tokens.First(x => x.TokenContract is null);

        var lockId = request.Id.ToBytes32();
        var secret = BigInteger.Parse(request.Secret);

        var htlcContractAddress = currency.IsNative
            ? network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        return new PrepareTransactionResponse
        {
            Data = new RedeemFunction
            {
                Id = lockId,
                Secret = secret
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),
            Amount = 0m,
            AmountInWei = "0",
            ToAddress = htlcContractAddress,
            Asset = nativeCurrency.Asset,
            CallDataAsset = request.Asset,
            CallDataAmount = 0,
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

        var nativeCurrency = network.Tokens.First(x => x.TokenContract is null);

        var htlcId = request.Id.ToBytes32();

        var htlcContractAddress = currency.IsNative
            ? network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        return new PrepareTransactionResponse
        {
            Data = new RefundFunction
            {
                Id = htlcId,
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),
            Amount = 0m,
            AmountInWei = "0",
            ToAddress = htlcContractAddress,
            Asset = nativeCurrency.Asset,
            CallDataAsset = request.Asset,
            CallDataAmount = 0,
            CallDataAmountInWei = "0",
        };
    }
}
