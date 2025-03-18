using FluentResults;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;
using System.Numerics;
using Train.Solver.Core.Blockchain.EVM.Extensions;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Extensions;
using Train.Solver.Data.Entities;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Train.Solver.Core.Blockchain.EVM.FunctionMessages;
using Train.Solver.Core.Blockchain.Models;

namespace Train.Solver.Core.Blockchain.EVM.Helpers;

public static class EVMTransactionBuilder
{
    public static Result<PrepareTransactionResponse> BuildApproveTransaction(Network network, string args)
    {
        var request = args.FromArgs<ApprovePrepareRequest>();

        var currency = network.Tokens.SingleOrDefault(x => x.Asset == request.Asset);

        if (currency is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Currency is not configured on {network.Name} network"));
        }

        var nativeCurrency = network.Tokens.Single(x => string.IsNullOrEmpty(x.TokenContract));

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
                    Spender = request.SpenderAddress,
                    Value = Web3.Convert.ToWei(request.Amount, currency.Decimals)
                }.GetCallData().ToHex().EnsureEvenLengthHex().EnsureHexPrefix(),
                CallDataAsset = nativeCurrency.Asset,
                CallDataAmountInWei = "0",
                CallDataAmount = 0
            };

            return Result.Ok(response);
        }
        else
        {
            return Result.Fail($"Requested currency does not have contract address");
        }
    }

    public static Result<PrepareTransactionResponse> BuildTransferTransaction(
    Network network,
    string args)
    {
        var request = args.FromArgs<TransferPrepareRequest>();

        var response = new PrepareTransactionResponse();
        string memoHex = "";

        if (!string.IsNullOrEmpty(request.Memo))
        {
            memoHex = BigInteger.Parse(request.Memo).ToHexBigInteger().HexValue;
            response.Data = memoHex;
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            return Result.Fail($"Currency {request.Asset} for {network.Name} is missing");
        }

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

    public static Result<PrepareTransactionResponse> BuildHTLCAddLockSigTransaction(Network network, string args)
    {
        var request = args.FromArgs<HTLCAddLockSigTransactionPrepareRequest>();


        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            return Result.Fail($"Currency {request.Asset} for {network.Name} is missing");
        }

        var htlcContractAddress = currency.IsNative
            ? network.DeployedContracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        var nativeCurrency = network.Tokens.FirstOrDefault(x => x.TokenContract is null);

        if (nativeCurrency == null)
        {
            return Result.Fail($"Native currency for {network.Name} is not setup");
        }

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

    public static Result<PrepareTransactionResponse> BuildHTLCCommitTransaction(Network network, string args)
    {
        var request = args.FromArgs<HTLCCommitTransactionPrepareRequest>();


        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.SourceAsset.ToUpper());

        if (currency is null)
        {
            return Result.Fail($"Currency {request.SourceAsset} for {network.Name} is missing");
        }

        var nativeCurrency = network.Tokens.FirstOrDefault(x => x.TokenContract is null);

        if (nativeCurrency == null)
        {
            return Result.Fail($"Native currency for {network.Name} is not setup");
        }

        var response = new PrepareTransactionResponse();

        //ERC20 HTLC call
        if (currency.Id != nativeCurrency.Id)
        {
            response.ToAddress = network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

            response.Data = new ERC20CreatePFunction
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

            response.Amount = 0;
            response.AmountInWei = "0";
            response.Asset = nativeCurrency.Asset;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmount = request.Amount;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount, currency.Decimals).ToString();
        }
        else
        {
            response.ToAddress = network.DeployedContracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address;

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

            response.Amount = request.Amount;
            response.AmountInWei = Web3.Convert.ToWei(request.Amount, nativeCurrency.Decimals).ToString();
            response.Asset = nativeCurrency.Asset;
            response.CallDataAsset = request.SourceAsset;
            response.CallDataAmount = request.Amount;
            response.CallDataAmountInWei = Web3.Convert.ToWei(request.Amount, nativeCurrency.Decimals).ToString();
        }

        return response;

    }

    public static Result<PrepareTransactionResponse> BuildHTLCLockTransaction(
    Network network,
    string args)
    {
        var request = args.FromArgs<HTLCLockTransactionPrepareRequest>();

        var response = new PrepareTransactionResponse();


        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.SourceAsset.ToUpper());

        if (currency is null)
        {
            return Result.Fail($"Currency {request.SourceAsset} for {network.Name} is missing");
        }

        var nativeCurrency = network.Tokens.FirstOrDefault(x => x.TokenContract is null);

        if (nativeCurrency == null)
        {
            return Result.Fail($"Native currency for {network.Name} is not setup");
        }

        var hashlock = request.Hashlock.ToBytes32();

        response.Asset = nativeCurrency.Asset;

        if (currency.Id != nativeCurrency.Id)
        {
            response.Data = new ERC20LockFunction
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
            ? network.DeployedContracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        response.ToAddress = htlcContractAddress;

        return response;
    }

    public static Result<PrepareTransactionResponse> BuildHTLCRedeemTranaction(
Network network, string args)
    {
        var request = args.FromArgs<HTLCRedeemTransactionPrepareRequest>();

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            return Result.Fail($"Currency {request.Asset} for {network.Name} is missing");
        }

        var nativeCurrency = network.Tokens.FirstOrDefault(x => x.TokenContract is null);

        if (nativeCurrency == null)
        {
            return Result.Fail($"Native currency for {network.Name} is not setup");
        }

        var lockId = request.Id.ToBytes32();
        var secret = BigInteger.Parse(request.Secret);

        var htlcContractAddress = currency.IsNative
            ? network.DeployedContracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

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

    public static Result<PrepareTransactionResponse> BuildHTLCRefundTransaction(Network network, string args)
    {
        var request = args.FromArgs<HTLCRefundTransactionPrepareRequest>();

        
        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (currency is null)
        {
            return Result.Fail($"Currency {request.Asset} for {network.Name} is missing");
        }

        var nativeCurrency = network.Tokens.FirstOrDefault(x => x.TokenContract is null);

        if (nativeCurrency == null)
        {
            return Result.Fail($"Native currency for {network.Name} is not setup");
        }

        var htlcId = request.Id.ToBytes32();

        var htlcContractAddress = currency.IsNative
            ? network.DeployedContracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

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
