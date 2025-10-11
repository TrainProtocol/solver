using Nethereum.ABI.EIP712;
using Nethereum.Contracts.Standards.ERC1271.ContractDefinition;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Mappers;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Util;
using Nethereum.Web3;
using RedLockNet;
using StackExchange.Redis;
using System.Numerics;
using Temporalio.Activities;
using Train.Solver.Common.Enums;
using Train.Solver.Common.Helpers;
using Train.Solver.Infrastructure.Abstractions;
using Train.Solver.Infrastructure.Abstractions.Exceptions;
using Train.Solver.Infrastructure.Abstractions.Models;
using Train.Solver.SmartNodeInvoker;
using Train.Solver.Workflow.Abstractions.Activities;
using Train.Solver.Workflow.Abstractions.Models;
using Train.Solver.Workflow.EVM.FunctionMessages;
using Train.Solver.Workflow.EVM.Helpers;
using Train.Solver.Workflow.EVM.Models;

namespace Train.Solver.Workflow.EVM.Activities;

public class EVMBlockchainActivities(
    IFeeEstimatorFactory feeEstimatorFactory,
    IDistributedLockFactory distributedLockFactory,
    ISmartNodeInvoker smartNodeInvoker,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) : IEVMBlockchainActivities, IBlockchainActivities
{
    private readonly string[] _nonRetriableErrors =
    [
        "nonce too low",
        "Nonce already used"
    ];

    private readonly string[] _insuficientFundsErrors =
    [
        "insufficient funds",
    ];

    private readonly string[] _replacementErrors =
    [
        "replacement transaction underpriced",
        "known transaction",
        "replace existing",
        "already known",
        "existing tx with same hash"
    ];

    [Activity]
    public virtual async Task<Fee> EstimateFeeAsync(EstimateFeeRequest request)
    {
        var feeEstimator = feeEstimatorFactory.Create(request.Network.FeeType);
        var fee = await feeEstimator.EstimateAsync(request);

        var balance = await GetBalanceAsync(new BalanceRequest
        {
            Network = request.Network,
            Address = request.FromAddress,
            Asset = fee.Asset
        });

        var amount = fee.Amount + request.Amount;

        if (balance.Amount < amount)
        {
            throw new Exception($"Insufficient funds in {request.Network.DisplayName}. {request.FromAddress}. Required {amount} {fee.Asset}");
        }

        return fee;
    }

    [Activity]
    public Task<Fee> IncreaseFeeAsync(EVMFeeIncreaseRequest request)
    {
        var feeEstimator = feeEstimatorFactory.Create(request.Network.FeeType);
        feeEstimator.Increase(request.Fee, request.Network.FeePercentageIncrease);

        return Task.FromResult(request.Fee);
    }

    [Activity]
    public virtual async Task<TransactionResponse> GetBatchTransactionAsync(GetBatchTransactionRequest request)
    {
        TransactionResponse? transaction = null;

        foreach (var transactionId in request.TransactionHashes)
        {
            transaction = await GetTransactionAsync(request.Network, transactionId);
        }

        if (transaction == null)
        {
            throw new TransactionNotComfirmedException("Transaction not confirmed");
        }

        return transaction;
    }

    [Activity]
    public virtual Task<PrepareTransactionDto> BuildTransactionAsync(TransactionBuilderRequest request)
    {
        PrepareTransactionDto result = request.Type switch
        {
            TransactionType.Transfer => EVMTransactionBuilder.BuildTransferTransaction(request.Network, request.PrepareArgs),
            TransactionType.Approve => EVMTransactionBuilder.BuildApproveTransaction(request.Network, request.PrepareArgs),
            TransactionType.HTLCCommit => EVMTransactionBuilder.BuildHTLCCommitTransaction(request.Network, request.PrepareArgs),
            TransactionType.HTLCLock => EVMTransactionBuilder.BuildHTLCLockTransaction(request.Network, request.PrepareArgs),
            TransactionType.HTLCRedeem => EVMTransactionBuilder.BuildHTLCRedeemTranaction(request.Network, request.PrepareArgs),
            TransactionType.HTLCRefund => EVMTransactionBuilder.BuildHTLCRefundTransaction(request.Network, request.PrepareArgs),
            TransactionType.HTLCAddLockSig => EVMTransactionBuilder.BuildHTLCAddLockSigTransaction(request.Network, request.PrepareArgs),
            _ => throw new ArgumentOutOfRangeException(nameof(request.Type),
                                $"Transaction type {request.Type} is not supported for network {request.Network.Name}"),
        };
        return Task.FromResult(result);
    }

    [Activity]
    public virtual async Task<BalanceResponse> GetBalanceAsync(BalanceRequest request)
    {
        var currency = request.Network.Tokens.Single(x => x.Symbol.ToUpper() == request.Asset.ToUpper());

        BigInteger balance;

        if (currency.Contract is null)
        {
            var result = await smartNodeInvoker.ExecuteAsync(
                request.Network.Name,
                request.Network.Nodes.Select(x => x.Url),
                async url =>
                    await new Web3(url).Eth.GetBalance.SendRequestAsync(request.Address));

            if (!result.Succeeded)
            {
                throw new AggregateException(result.FailedNodes.Values);
            }

            balance = result.Data;
        }
        else
        {
            var result = await smartNodeInvoker.ExecuteAsync(request.Network.Name, request.Network.Nodes.Select(x => x.Url),
                async url => await new Web3(url).Eth.GetContractQueryHandler<BalanceOfFunction>()
                    .QueryAsync<BigInteger>(currency.Contract, new() { Owner = request.Address }));

            if (!result.Succeeded)
            {
                throw new AggregateException(result.FailedNodes.Values);
            }

            balance = result.Data;
        }

        var balanceResponse = new BalanceResponse
        {
            Amount = balance,
        };

        return balanceResponse;
    }

    [Activity]
    public virtual async Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request)
    {
        var result = new HTLCBlockEventResponse();

        var nodes = request.Network.Nodes.Select(x => x.Url);

        if (!nodes.Any())
        {
            throw new Exception($"Node is not configured on {request.Network.Name} network");
        }

        var contractAddresses = new List<string>();

        if (!string.IsNullOrEmpty(request.Network.HTLCNativeContractAddress))
        {
            contractAddresses.Add(request.Network.HTLCNativeContractAddress);
        }

        if (!string.IsNullOrEmpty(request.Network.HTLCTokenContractAddress))
        {
            contractAddresses.Add(request.Network.HTLCTokenContractAddress);
        }

        var filterInput = new NewFilterInput
        {
            FromBlock = new BlockParameter(request.FromBlock),
            ToBlock = new BlockParameter(request.ToBlock),
            Address = [.. contractAddresses],
        };

        var logsResult = await smartNodeInvoker.ExecuteAsync(request.Network.Name, nodes,
            async url =>
                await new Web3(url).Eth.Filters.GetLogs
                    .SendRequestAsync(filterInput));

        if (!logsResult.Succeeded)
        {
            throw new AggregateException(logsResult.FailedNodes.Values);
        }

        foreach (var log in logsResult.Data)
        {
            var decodedEvent = EventDecoder.Decode(log);

            if (decodedEvent == null)
            {
                continue;
            }

            var (eventType, typedEvent) = decodedEvent.Value;

            if (eventType == typeof(EtherTokenCommittedEvent) ||
                decodedEvent.Value.eventType == typeof(ERC20TokenCommitedEvent))
            {
                var commitedEvent = (EtherTokenCommittedEvent)typedEvent;

                var wallet = request.WalletAddresses.Where(x =>
                    FormatAddress(x) == FormatAddress(commitedEvent.Receiver)).FirstOrDefault();

                if (wallet == null)
                {
                    continue;
                }

                var commitId = commitedEvent.Id.ToHex(prefix: true);

                var message = new HTLCCommitEventMessage
                {
                    TxId = log.TransactionHash,
                    CommitId = commitId,
                    Amount = commitedEvent.Amount,
                    SourceAsset = commitedEvent.SourceAsset,
                    SenderAddress = commitedEvent.Sender,
                    SourceNetwork = request.Network.Name,
                    DestinationAddress = commitedEvent.DestinationAddress,
                    DestinationNetwork = commitedEvent.DestinationChain,
                    DestinationAsset = commitedEvent.DestinationAsset,
                    TimeLock = (long)commitedEvent.Timelock,
                    ReceiverAddress = FormatAddress(wallet),
                };

                result.HTLCCommitEventMessages.Add(message);
            }
            else if (eventType == typeof(EtherTokenLockAddedEvent))
            {
                var lockAddedEvent = (EtherTokenLockAddedEvent)typedEvent;
                var commitId = lockAddedEvent.Id;

                var message = new HTLCLockEventMessage
                {
                    TxId = log.TransactionHash,
                    CommitId = commitId.ToHex().EnsureHexPrefix(),
                    HashLock = lockAddedEvent.Hashlock.ToHex().EnsureHexPrefix(),
                    TimeLock = (long)lockAddedEvent.Timelock,
                };

                result.HTLCLockEventMessages.Add(message);
            }
        }

        return result;
    }

    [Activity]
    public virtual async Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request)
    {
        var nodes = request.Network.Nodes.Select(x => x.Url);

        if (!nodes.Any())
        {
            throw new Exception($"Node is not configured on {request.Network.Name} network");
        }

        var blockResult = await smartNodeInvoker.ExecuteAsync(request.Network.Name, nodes,
            async url =>
                await new Web3(url).Eth.Blocks.GetBlockWithTransactionsHashesByNumber
                    .SendRequestAsync(BlockParameter.CreateLatest()));

        if (!blockResult.Succeeded)
        {
            throw new AggregateException(blockResult.FailedNodes.Values);
        }

        return new()
        {
            BlockNumber = (ulong)blockResult.Data.Number.Value,
            BlockHash = blockResult.Data.BlockHash,
        };
    }

    [Activity]
    public virtual async Task<string> GetSpenderAllowanceAsync(AllowanceRequest request)
    {
        var nodes = request.Network.Nodes.Select(x => x.Url);

        if (!nodes.Any())
        {
            throw new Exception($"Node is not configured on {request.Network.Name} network");
        }

        var currency = request.Network.Tokens.Single(x => x.Symbol == request.Asset);

        var isNative = currency.Symbol == request.Network.NativeToken!.Symbol;

        var htlcContractAddress = isNative
            ? request.Network.HTLCNativeContractAddress
            : request.Network.HTLCTokenContractAddress;

        if (!string.IsNullOrEmpty(currency.Contract))
        {
            var allowanceFunctionMessage = new AllowanceFunction
            {
                Owner = request.OwnerAddress,
                Spender = htlcContractAddress,
            };

            var allowanceResult = await smartNodeInvoker.ExecuteAsync(request.Network.Name, nodes,
                async url =>
                    await new Web3(url).Eth.GetContractQueryHandler<AllowanceFunction>().QueryAsync<BigInteger>(currency.Contract, allowanceFunctionMessage));

            if (!allowanceResult.Succeeded)
            {
                throw new AggregateException(allowanceResult.FailedNodes.Values);
            }

            return allowanceResult.Data.ToString();
        }

        return (BigInteger.Pow(2, 256) - 1).ToString();
    }

    [Activity]
    public virtual async Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request)
    {
        var currency = request.Network.Tokens.Single(x => x.Symbol.ToUpper() == request.Asset.ToUpper());

        var isNative = currency.Symbol == request.Network.NativeToken!.Symbol;

        var htlcContractAddress = isNative
            ? request.Network.HTLCNativeContractAddress
            : request.Network.HTLCTokenContractAddress;

        var signer = new Eip712TypedDataSigner();
        var typedData = GetAddLockMessageTypedDefinition(
            long.Parse(request.Network.ChainId!),
            htlcContractAddress);

        var addLockMsg = new AddLockMessage
        {
            Hashlock = request.Hashlock.HexToByteArray(),
            Id = request.CommitId.HexToByteArray(),
            Timelock = (ulong)request.Timelock
        };

        var codeResult = await smartNodeInvoker.ExecuteAsync(request.Network.Name, request.Network.Nodes.Select(x => x.Url),
            async url => await new Web3(url).Eth.GetCode.SendRequestAsync(request.SignerAddress));

        if (!codeResult.Succeeded)
        {
            throw new AggregateException(codeResult.FailedNodes.Values);
        }

        // TODO, make sure this is compatibly with Pectra update
        // https://ithaca.xyz/updates/exp-0001

        // EOA
        if (string.IsNullOrEmpty(codeResult.Data) || codeResult.Data == "0x")
        {
            var signature = EthECDSASignatureFactory.FromComponents(
                request.R.HexToByteArray(),
                request.S.HexToByteArray(),
                byte.Parse(request.V)
            );

            var sign = EthECDSASignature.CreateStringSignature(signature);
            var addressRecovered = signer.RecoverFromSignatureV4(addLockMsg, typedData, sign);

            return string.Equals(addressRecovered, request.SignerAddress, StringComparison.OrdinalIgnoreCase);

            // Assume https://eips.ethereum.org/EIPS/eip-1271
        }
        else
        {
            var isValidSignatureFunction = new IsValidSignatureFunction
            {
                Hash = Sha3Keccack.Current.CalculateHash(signer.EncodeTypedData(addLockMsg, typedData)),
                Signature = request.Signature.HexToByteArray()
            };

            var isValidSignatureHResult = await smartNodeInvoker.ExecuteAsync(request.Network.Name, request.Network.Nodes.Select(x => x.Url),
                async url => await new Web3(url).Eth.GetContractQueryHandler<IsValidSignatureFunction>()
                .QueryAsync<byte[]>(request.SignerAddress, isValidSignatureFunction));

            if (!isValidSignatureHResult.Succeeded)
            {
                throw new AggregateException(isValidSignatureHResult.FailedNodes.Values);
            }

            if (isValidSignatureHResult.Data == null)
            {
                throw new Exception($"Failed to get {request.SignerAddress} IsValidSignatureFunction query handler in {request.Network.Name}");
            }

            var isValidSignatureMagicValue = isValidSignatureHResult.Data.ToHex();

            // magic value: https://docs.uniswap.org/contracts/v3/reference/periphery/interfaces/external/IERC1271
            return string.Equals(isValidSignatureMagicValue, "1626ba7e", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Activity]
    public virtual async Task<string> GetNextNonceAsync(NextNonceRequest request)
    {
        var nodes = request.Network.Nodes.Select(x => x.Url);

        request.Address = FormatAddress(request.Address);

        await using var distributedLock = await distributedLockFactory.CreateLockAsync(
            resource: RedisHelper.BuildLockKey(request.Network.Name, request.Address),
            retryTime: TimeSpan.FromSeconds(1),
            waitTime: TimeSpan.FromSeconds(20),
            expiryTime: TimeSpan.FromSeconds(25));

        if (!distributedLock.IsAcquired)
        {
            throw new Exception("Failed to acquire the lock");
        }

        var curentNonce = new BigInteger(-1);

        var currentNonceRedis = await cache.StringGetAsync(RedisHelper.BuildNonceKey(request.Network.Name, request.Address));

        if (currentNonceRedis != RedisValue.Null)
        {
            curentNonce = BigInteger.Parse(currentNonceRedis!);
        }

        var nonceResult = await smartNodeInvoker.ExecuteAsync(request.Network.Name, nodes,
            async url =>
                await new Web3(url).Eth.Transactions.GetTransactionCount
                    .SendRequestAsync(request.Address, BlockParameter.CreatePending()));

        if (!nonceResult.Succeeded)
        {
            throw new AggregateException(nonceResult.FailedNodes.Values);
        }

        if (nonceResult.Data <= curentNonce)
        {
            curentNonce++;
            nonceResult.Data = new HexBigInteger(curentNonce);
        }
        else
        {
            curentNonce = nonceResult.Data;
        }

        await cache.StringSetAsync(RedisHelper.BuildNonceKey(request.Network.Name, request.Address),
            curentNonce.ToString(),
            expiry: TimeSpan.FromDays(7));

        return nonceResult.Data.ToString();
    }

    [Activity]
    public virtual async Task<string> PublishRawTransactionAsync(EVMPublishTransactionRequest request)
    {
        var result = await smartNodeInvoker.ExecuteAsync(request.Network.Name, request.Network.Nodes.Select(x => x.Url),
            async url => await new
                    EthSendRawTransaction(new Web3(url).Client)
                .SendRequestAsync(request.SignedTransaction.RawTxn));

        if (!result.Succeeded)
        {
            TransactionResponse? transactionResponse = null;

            try
            {
                transactionResponse = await GetTransactionAsync(request.Network, request.SignedTransaction.Hash);

                if (transactionResponse != null)
                {
                    return transactionResponse.TransactionHash.EnsureHexPrefix();
                }
            }
            catch (TransactionNotComfirmedException)
            {
                return request.SignedTransaction.Hash;
            }
            catch (Exception)
            {
                // swallow exception and continue to process the original error
            }

            foreach (var innerEx in result.FailedNodes.Values)
            {
                if (innerEx is RpcResponseException exNonceTooLow
                    && _nonRetriableErrors.Any(x =>
                        exNonceTooLow.Message.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                {
                    // Assuming if nonce too low then the transaction is already confirmed on blockchain and we have to return txId
                    break;
                }

                if (innerEx is RpcResponseException exInsuffFunds && _insuficientFundsErrors.Any(x =>
                        exInsuffFunds.Message.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                {
                    throw new Exception(
                        $"Insufficient funds in {request.Network.Name}. {request.FromAddress}. Message {exInsuffFunds.Message}",
                        innerEx);
                }

                if (innerEx is Exception exReplacement
                    && _replacementErrors.Any(x =>
                        exReplacement.Message.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                {
                    throw new TransactionUnderpricedException("Transaction underpriced");
                }

                if (innerEx is Exception exGeneral)
                {
                    throw new Exception(
                        $"Send raw transaction failed due to error(s): {string.Join('\t', result.FailedNodes.Values.Select(c => c.Message))}",
                        innerEx);
                }
            }

            return result.Data;
        }

        return request.SignedTransaction.Hash.EnsureHexPrefix();
    }

    [Activity]
    public virtual async Task<SignedTransaction> ComposeSignedRawTransactionAsync(EVMComposeTransactionRequest request)
    {
        var transactionInput = new TransactionInput
        {
            ChainId = BigInteger.Parse(request.Network.ChainId).ToHexBigInteger(),
            From = request.FromAddress,
            To = request.ToAddress,
            Nonce = BigInteger.Parse(request.Nonce).ToHexBigInteger(),
            Value = request.Amount.ToHexBigInteger(),
            Data = request.CallData?.EnsureHexPrefix()
        };

        if (request.Fee?.LegacyFeeData is not null)
        {
            transactionInput.GasPrice = request.Fee.LegacyFeeData.GasPrice.ToHexBigInteger();
            transactionInput.Gas = request.Fee.LegacyFeeData.GasLimit.ToHexBigInteger();
        }
        else if (request.Fee?.Eip1559FeeData is not null)
        {
            var maxFeePerGas = request.Fee.Eip1559FeeData.MaxFeePerGas;
            var maxPriorityFeePerGas = request.Fee.Eip1559FeeData.MaxPriorityFee;

            transactionInput.Gas = request.Fee.Eip1559FeeData.GasLimit.ToHexBigInteger();
            transactionInput.MaxFeePerGas = maxFeePerGas.ToHexBigInteger();
            transactionInput.MaxPriorityFeePerGas = maxPriorityFeePerGas.ToHexBigInteger();
            transactionInput.Type = new HexBigInteger((int)Nethereum.Model.TransactionType.EIP1559);
        }

        var chainId = transactionInput.ChainId;


        var gasLimit = transactionInput.Gas;
        var value = transactionInput.Value ?? new HexBigInteger(0);

        if (chainId == null) throw new Exception("ChainId required for TransactionType 0X02 EIP1559");

        string unsignedRawTransaction;

        if (transactionInput.Type != null && transactionInput.Type.Value == Nethereum.Model.TransactionTypeExtensions.AsByte(Nethereum.Model.TransactionType.EIP1559))
        {
            var maxPriorityFeePerGas = transactionInput.MaxPriorityFeePerGas.Value;
            var maxFeePerGas = transactionInput.MaxFeePerGas.Value;

            var transaction1559 = new Nethereum.Model.Transaction1559(
                chainId.Value,
                transactionInput.Nonce,
                maxPriorityFeePerGas,
                maxFeePerGas,
                gasLimit,
                transactionInput.To,
                value,
                transactionInput.Data,
                transactionInput.AccessList.ToSignerAccessListItemArray());

            unsignedRawTransaction = transaction1559.GetRLPEncodedRaw().ToHex().EnsureHexPrefix();
        }
        else
        {
            var transactionLegacy = new Nethereum.Model.LegacyTransactionChainId(
                transactionInput.To,
                value.Value,
                transactionInput.Nonce,
                transactionInput.GasPrice.Value,
                gasLimit.Value,
                transactionInput.Data,
                chainId.Value);

            unsignedRawTransaction = transactionLegacy.GetRLPEncodedRaw().ToHex().EnsureHexPrefix();
        }

        var signedTransaction = await privateKeyProvider.SignAsync(
            request.SignerAgentUrl,
            request.Network.Type,
            request.FromAddress,
            unsignedRawTransaction);

        if (string.IsNullOrEmpty(signedTransaction))
        {
            throw new Exception($"Failed to sign transaction for {request.FromAddress} on {transactionInput.ChainId}");
        }

        var decodedTransaction = Nethereum.Model.TransactionFactory.CreateTransaction(signedTransaction);

        var txHash = decodedTransaction.Hash.ToHex();

        return new SignedTransaction
        {
            RawTxn = signedTransaction,
            Hash = txHash,
        };
    }

    private static string FormatAddress(string address) => address.ToLower();

    private static bool ValidateAddress(string address)
    {
        return AddressUtil.Current.IsValidEthereumAddressHexFormat(
            FormatAddress(address));
    }

    private static TypedData<Domain> GetAddLockMessageTypedDefinition(
        long chainId, string verifyingContract)
    {
        return new TypedData<Domain>
        {
            Domain = new Domain
            {
                Name = "Train",
                Version = "1",
                ChainId = chainId,
                VerifyingContract = verifyingContract
            },
            Types = MemberDescriptionFactory.GetTypesMemberDescription(typeof(Domain), typeof(AddLockMessage)),
            PrimaryType = "addLockMsg",
        };
    }

    private async Task<TransactionResponse> GetTransactionAsync(DetailedNetworkDto network, string transactionId)
    {
        var nodes = network.Nodes.Select(x => x.Url);

        if (!nodes.Any())
        {
            throw new Exception($"Node is not configured on {network.Name} network");
        }

        var nativeCurrency = network.Tokens
            .Single(x => x.Contract is null);

        var transactionResult = await smartNodeInvoker.ExecuteAsync(network.Name, nodes,
                async url =>
                    await new Web3(url).Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionId));

        if (!transactionResult.Succeeded)
        {
            throw new AggregateException(transactionResult.FailedNodes.Values);
        }

        if (transactionResult.Data is null)
        {
            throw new Exception($"Transaction {transactionId} not found on {network.Name}");
        }

        if (transactionResult.Data.BlockNumber is null)
        {
            throw new TransactionNotComfirmedException($"Transaction not confirmed yet on {network.Name}");
        }

        if (string.IsNullOrEmpty(transactionResult.Data.To))
        {
            throw new Exception($"Transaction recipient is missing block number: {transactionResult.Data.BlockNumber}");
        }

        var currentBlockNumberResult = await smartNodeInvoker.ExecuteAsync(network.Name, nodes,
            async url =>
                await new Web3(url).Eth.Blocks.GetBlockNumber.SendRequestAsync());

        if (!currentBlockNumberResult.Succeeded)
        {
            throw new AggregateException(currentBlockNumberResult.FailedNodes.Values);
        }

        var transactionBlockResult = await smartNodeInvoker.ExecuteAsync(network.Name, nodes,
            async url =>
                await new Web3(url).Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(transactionResult.Data.BlockNumber));

        if (!transactionBlockResult.Succeeded)
        {
            throw new AggregateException(transactionBlockResult.FailedNodes.Values);
        }

        var transactionReceiptResult = await smartNodeInvoker.ExecuteAsync(network.Name, nodes,
            async nodeUrl => await new Web3(nodeUrl).Client
                .SendRequestAsync<EVMTransactionReceipt>(
                    new RpcRequest(
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        "eth_getTransactionReceipt",
                        transactionResult.Data.TransactionHash)));

        if (transactionReceiptResult is null)
        {
            throw new Exception("Failed to get receipt. Receipt was null");
        }

        if (!transactionReceiptResult.Data.Succeeded())
        {
            throw new TransactionFailedException("Transaction failed");
        }

        var feeEstimator = feeEstimatorFactory.Create(network.FeeType);
        var transactionFee = feeEstimator.CalculateFee(
            transactionBlockResult.Data,
            transactionResult.Data,
            transactionReceiptResult.Data);

        var from = FormatAddress(transactionResult.Data.From);
        var to = FormatAddress(transactionResult.Data.To);

        var transactionModel = new TransactionResponse
        {
            Decimals = nativeCurrency.Decimals,
            NetworkName = network.Name,
            Status = TransactionStatus.Completed,
            TransactionHash = transactionResult.Data.TransactionHash,
            FeeAmount = transactionFee,
            FeeDecimals = nativeCurrency.Decimals,
            FeeAsset = nativeCurrency!.Symbol,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)transactionBlockResult.Data.Timestamp.Value * 1000),
            Confirmations = (int)(currentBlockNumberResult.Data.Value - transactionResult.Data.BlockNumber.Value) + 1
        };

        return transactionModel;
    }
}
