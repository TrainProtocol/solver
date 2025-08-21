using System.Numerics;
using Nethereum.Contracts;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Train.Solver.Workflow.EVM.FunctionMessages;
using Train.Solver.Common.Extensions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.Workflow.Abstractions.Models;

namespace Train.Solver.Workflow.EVM.Helpers;

public static class EVMTransactionBuilder
{
    public static PrepareTransactionDto BuildApproveTransaction(DetailedNetworkDto network, string args)
    {
        var request = args.FromJson<ApprovePrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.Single(x => x.Symbol == request.Asset);

        var nativeCurrency = network.Tokens.Single(x => string.IsNullOrEmpty(x.Contract));

        var isNative = currency.Symbol == network.NativeToken!.Symbol;

        var htlcContractAddress = isNative
            ? network.HTLCNativeContractAddress
            : network.HTLCTokenContractAddress;

        if (!isNative)
        {
            var response = new PrepareTransactionDto
            {
                ToAddress = currency.Contract!,
                Amount = BigInteger.Zero,
                Asset = nativeCurrency.Symbol,
                Data = new FunctionMessages.ApproveFunction
                {
                    Spender = htlcContractAddress,
                    Value = request.Amount
                }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),
                CallDataAsset = nativeCurrency.Symbol,
                CallDataAmount = BigInteger.Zero,
            };

            return response;
        }

        throw new Exception("Requested currency does not have contract address");
    }

    public static PrepareTransactionDto BuildTransferTransaction(
        DetailedNetworkDto network,
        string args)
    {
        var request = args.FromJson<TransferPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var response = new PrepareTransactionDto();
      
        var currency = network.Tokens.Single(x => x.Symbol.ToUpper() == request.Asset.ToUpper());

        response.Asset = network.NativeToken!.Symbol;

        if (currency.Symbol != network.NativeToken!.Symbol)
        {
            response.Data = new TransferFunction
            {
                To = request.ToAddress,
                Value = request.Amount,
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.Amount = BigInteger.Zero;
            response.ToAddress = currency.Contract!;
            response.CallDataAsset = currency.Symbol;
            response.CallDataAmount = request.Amount;
        }
        else
        {
            response.Amount = request.Amount;
            response.ToAddress = request.ToAddress;
            response.CallDataAsset = currency.Symbol;
            response.CallDataAmount = request.Amount;
        }

        return response;
    }

    public static PrepareTransactionDto BuildHTLCAddLockSigTransaction(DetailedNetworkDto network, string args)
    {
        var request = args.FromJson<AddLockSigTransactionPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.Single(x => x.Symbol.ToUpper() == request.Asset.ToUpper());
        var isNative = currency.Symbol == network.NativeToken!.Symbol;

        var htlcContractAddress = isNative
            ? network.HTLCNativeContractAddress
            : network.HTLCTokenContractAddress;

        var nativeCurrency = network.Tokens.First(x => x.Contract is null);

        var hashlock = request.Hashlock.ToBytes32();

        var response = new PrepareTransactionDto
        {
            Data = new AddLockSigFunction
            {
                Message = new AddLockMessage
                {
                    Id = request.CommitId.ToBytes32(),
                    Hashlock = hashlock,
                    Timelock = (ulong)request.Timelock
                },
                R = request.R!.ToBytes32(),
                S = request.S!.ToBytes32(),
                V = byte.Parse(request.V!)
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),

            Amount = BigInteger.Zero,
            Asset = nativeCurrency.Symbol,
            ToAddress = htlcContractAddress,
            CallDataAsset = request.Asset,
            CallDataAmount = BigInteger.Zero
        };

        return response;
    }

    public static PrepareTransactionDto BuildHTLCCommitTransaction(DetailedNetworkDto network, string args)
    {
        var request = args.FromJson<HTLCCommitTransactionPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.Single(x => x.Symbol.ToUpper() == request.SourceAsset.ToUpper());

        var nativeCurrency = network.Tokens.First(x => x.Contract is null);

        var response = new PrepareTransactionDto();

        //ERC20 HTLC call
        if (currency.Symbol != nativeCurrency.Symbol)
        {
            response.ToAddress = network.HTLCTokenContractAddress;

            response.Data = new ERC20CommitFunction
            {
                Id = request.Id.ToBytes32(),
                Chains = request.HopChains,
                DestinationAddresses = request.HopAddresses,
                DestinationChain = request.DestinationChain,
                DestinationAsset = request.DestinationAsset,
                DestinationAddress = request.DestinationAddress,
                SourceAsset = request.SourceAsset,
                Receiver = request.Receiver,
                Timelock = request.Timelock,
                Amount = request.Amount,
                TokenContract = currency.Contract!
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.Amount = BigInteger.Zero;
            response.Asset = nativeCurrency.Symbol;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmount = request.Amount;
        }
        else
        {
            response.ToAddress = network.HTLCNativeContractAddress;

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
                Receiver = request.Receiver,
                Timelock = request.Timelock,
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.Amount = request.Amount;
            response.Asset = nativeCurrency.Symbol;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmount = request.Amount;
        }

        return response;

    }

    public static PrepareTransactionDto BuildHTLCLockTransaction(
        DetailedNetworkDto network,
        string args)
    {
        var request = args.FromJson<HTLCLockTransactionPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var response = new PrepareTransactionDto();

        var currency = network.Tokens.Single(x => x.Symbol.ToUpper() == request.SourceAsset.ToUpper());
        var isNative = currency.Symbol == network.NativeToken!.Symbol;

        var nativeCurrency = network.Tokens.First(x => x.Contract is null);

        var hashlock = request.Hashlock.ToBytes32();

        response.Asset = nativeCurrency.Symbol;

        if (!isNative)
        {
            response.Data = new ERC20LockFunction
            {
                LockParams = new ERC20LockMessage
                {
                    CommitId = request.CommitId.ToBytes32(),
                    SourceReceiver = request.Receiver,
                    Hashlock = hashlock,
                    Timelock = request.Timelock,
                    SourceAsset = request.SourceAsset,
                    DestinationChain = request.DestinationNetwork,
                    DestinationAddress = request.DestinationAddress,
                    DestinationAsset = request.DestinationAsset,
                    Reward = request.Reward,
                    RewardTimelock = request.RewardTimelock,
                    Amount = request.Amount,
                    TokenContract = currency.Contract!,
                }
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.Amount = BigInteger.Zero;
            response.Asset = nativeCurrency.Symbol;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmount = request.Amount + request.Reward;
        }
        else
        {
            response.Data = new LockFunction
            {
                Id = request.CommitId.ToBytes32(),
                Hashlock = hashlock,
                Timelock = request.Timelock,
                SourceReceiver = request.Receiver,
                SourceAsset = request.SourceAsset,
                DestinationChain = request.DestinationNetwork,
                DestinationAddress = request.DestinationAddress,
                DestinationAsset = request.DestinationAsset,
                Reward = request.Reward,
                RewardTimelock = request.RewardTimelock,
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix();

            response.Amount = request.Amount;
            response.Asset = nativeCurrency.Symbol;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmount = request.Amount + request.Reward;
        }

        var htlcContractAddress = isNative
            ? network.HTLCNativeContractAddress
            : network.HTLCTokenContractAddress;

        response.ToAddress = htlcContractAddress;

        return response;
    }

    public static PrepareTransactionDto BuildHTLCRedeemTranaction(
        DetailedNetworkDto network,
        string args)
    {
        var request = args.FromJson<HTLCRedeemTransactionPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.Single(x => x.Symbol.ToUpper() == request.Asset.ToUpper());
        var isNative = currency.Symbol == network.NativeToken!.Symbol;

        var lockId = request.CommitId.ToBytes32();
        var secret = BigInteger.Parse(request.Secret);

        var htlcContractAddress = isNative
            ? network.HTLCNativeContractAddress
            : network.HTLCTokenContractAddress;

        return new PrepareTransactionDto
        {
            Data = new RedeemFunction
            {
                Id = lockId,
                Secret = secret
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),
            Amount = BigInteger.Zero,
            ToAddress = htlcContractAddress,
            Asset = network.NativeToken.Symbol,
            CallDataAsset = request.Asset,
            CallDataAmount = BigInteger.Zero,
        };
    }

    public static PrepareTransactionDto BuildHTLCRefundTransaction(DetailedNetworkDto network, string args)
    {
        var request = args.FromJson<HTLCRefundTransactionPrepareRequest>();

        if (request is null)
        {
            throw new Exception($"Occured exception during deserializing {args}");
        }

        var currency = network.Tokens.Single(x => x.Symbol.ToUpper() == request.Asset.ToUpper());
        var isNative = currency.Symbol == network.NativeToken!.Symbol;

        var htlcId = request.CommitId.ToBytes32();

        var htlcContractAddress = isNative
            ? network.HTLCNativeContractAddress
            : network.HTLCTokenContractAddress;

        return new PrepareTransactionDto
        {
            Data = new RefundFunction
            {
                Id = htlcId,
            }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),
            Amount = BigInteger.Zero,
            ToAddress = htlcContractAddress,
            Asset = network.NativeToken.Symbol,
            CallDataAsset = request.Asset,
            CallDataAmount = BigInteger.Zero,
        };
    }
}
