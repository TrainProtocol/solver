using System.Numerics;
using Nethereum.ABI.EIP712;
using Nethereum.Contracts.Standards.ERC20.ContractDefinition;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Eth.Transactions;
using Nethereum.Signer;
using Nethereum.Signer.EIP712;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using RedLockNet;
using Serilog;
using StackExchange.Redis;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Entities;
using static Train.Solver.Core.Workflows.Helpers.ResilientNodeHelper;
using static Train.Solver.Blockchains.EVM.Helpers.EVMResilientNodeHelper;
using Train.Solver.Core.Models;
using Nethereum.RPC.Eth.Mappers;
using Train.Solver.Core.Models.HTLCModels;
using Train.Solver.Core.Services;
using Temporalio.Activities;
using Train.Solver.Blockchains.EVM.Models;
using Train.Solver.Blockchains.EVM.Helpers;
using Train.Solver.Blockchains.EVM.FunctionMessages;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Workflows.Helpers;
using Train.Solver.Core.Repositories;

namespace Train.Solver.Blockchains.EVM.Activities;

public class EVMBlockchainActivities(
    ISwapRepository swapRepository,
    INetworkRepository networkRepository,
    IDistributedLockFactory distributedLockFactory,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) : BlockchainActivitiesBase(networkRepository, swapRepository), IEVMBlockchainActivities
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

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(EstimateFeeAsync)}")]
    public override async Task<Fee> EstimateFeeAsync(EstimateFeeRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var feeEstimator = FeeEstimatorFactory.Create(network.FeeType);
        var fee = await feeEstimator.EstimateAsync(network, request);

        return fee;
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(IncreaseFeeAsync)}")]
    public async Task<Fee> IncreaseFeeAsync(EVMFeeIncreaseRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var feeEstimator = FeeEstimatorFactory.Create(network.FeeType);
        feeEstimator.Increase(request.Fee, network.FeePercentageIncrease);

        return request.Fee;
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(GetTransactionAsync)}")]
    public override async Task<TransactionResponse> GetTransactionAsync(GetTransactionRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var transaction = await GetTransactionAsync(network, request.TransactionId);

        if (transaction == null)
        {
            throw new TransactionNotComfirmedException("Transaction not found");
        }

        return transaction;
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(GetBatchTransactionAsync)}")]
    public virtual async Task<TransactionResponse> GetBatchTransactionAsync(GetBatchTransactionRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        TransactionResponse? transaction = null;

        foreach (var transactionId in request.TransactionIds)
        {
            transaction = await GetTransactionAsync(network, transactionId);
        }

        if (transaction == null)
        {
            throw new TransactionNotComfirmedException("Transaction not confirmed");
        }

        return transaction;
    }

    private async Task<TransactionResponse> GetTransactionAsync(Network network, string transactionId)
    {
        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new ArgumentException($"Node is not configured on {network.Name} network", nameof(nodes));
        }

        var nativeCurrency = network.Tokens
            .Single(x => x.TokenContract is null);

        var transaction = await GetDataFromNodesAsync(nodes,
                async url =>
                    await new Web3(url).Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionId));

        if (transaction is null)
        {
            return null;
        }

        if (transaction.BlockNumber is null)
        {
            throw new TransactionNotComfirmedException($"Transaction not confirmed yet on {network.Name}");
        }

        if (string.IsNullOrEmpty(transaction.To))
        {
            throw new Exception($"Transaction recipient is missing block number: {transaction.BlockNumber}");
        }

        var currentBlockNumber = await GetDataFromNodesAsync(nodes,
            async url =>
                await new Web3(url).Eth.Blocks.GetBlockNumber.SendRequestAsync());

        var transactionBlock = await GetDataFromNodesAsync(nodes,
            async url =>
                await new Web3(url).Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(transaction
                    .BlockNumber));

        if (transactionBlock == null || currentBlockNumber == null)
        {
            throw new Exception($"Failed to retrieve block");
        }

        var transactionReceiptResult =
            await GetTransactionReceiptAsync(nodes, transaction.TransactionHash);

        var feeEstimator = FeeEstimatorFactory.Create(network.FeeType);
        var transactionFee = feeEstimator.CalculateFee(
            transactionBlock,
            transaction,
            transactionReceiptResult);

        var from = FormatAddress(new() { Address = transaction.From });
        var to = FormatAddress(new() { Address = transaction.To });

        var transactionModel = new TransactionResponse
        {
            NetworkName = network.Name,
            Status = TransactionStatus.Completed,
            TransactionHash = transaction.TransactionHash,
            FeeAmount = Web3.Convert.FromWei(transactionFee, nativeCurrency.Decimals),
            FeeAsset = nativeCurrency!.Asset,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)transactionBlock.Timestamp.Value * 1000),
            Confirmations = (int)(currentBlockNumber.Value - transaction.BlockNumber.Value) + 1
        };

        return transactionModel;
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(BuildTransactionAsync)}")]
    public override async Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        PrepareTransactionResponse result;

        switch (request.Type)
        {
            case TransactionType.Transfer:
                result = EVMTransactionBuilder.BuildTransferTransaction(network, request.Args);
                break;
            case TransactionType.Approve:
                result = EVMTransactionBuilder.BuildApproveTransaction(network, request.Args);

                break;
            case TransactionType.HTLCCommit:
                result = EVMTransactionBuilder.BuildHTLCCommitTransaction(network, request.Args);

                break;
            case TransactionType.HTLCLock:
                result = EVMTransactionBuilder.BuildHTLCLockTransaction(network, request.Args);

                break;
            case TransactionType.HTLCRedeem:
                result = EVMTransactionBuilder.BuildHTLCRedeemTranaction(network, request.Args);

                break;
            case TransactionType.HTLCRefund:
                result = EVMTransactionBuilder.BuildHTLCRefundTransaction(network, request.Args);

                break;
            case TransactionType.HTLCAddLockSig:
                result = EVMTransactionBuilder.BuildHTLCAddLockSigTransaction(network, request.Args);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.Type),
                    $"Transaction type {request.Type} is not supported for network {network.Name}");
        }

        return result;
    }

    protected override string FormatAddress(AddressRequest request) => request.Address.ToLower();

    protected override bool ValidateAddress(AddressRequest request)
    {
        return AddressUtil.Current.IsValidEthereumAddressHexFormat(
            FormatAddress(
                new()
                {
                    Address = request.Address
                }));
    }
    
    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(GetBalanceAsync)}")]
    public override async Task<BalanceResponse> GetBalanceAsync(BalanceRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        if (network is null)
        {
            throw new ArgumentException($"Chain is not configured for {request.NetworkName} network");
        }

        // var primaryNode = network.Nodes.FirstOrDefault(x => x.Type == NodeType.Primary);
        //
        //  if (primaryNode is null)
        //  {
        //      return Result.Fail(
        //              new NotFoundError(
        //                  $"Primary node is not configured on {networkName} network"));
        //  }
        //
        //  var web3 = new Web3(primaryNode.Url);

        var currency = network.Tokens.Single(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        BigInteger balance;

        if (currency.TokenContract is null)
        {
            var result = await GetDataFromNodesAsync(network.Nodes,
                async url =>
                    await new Web3(url).Eth.GetBalance.SendRequestAsync(request.Address));

            balance = result.Value;
            //(await web3.Eth.GetBalance.SendRequestAsync(address)).Value;
        }
        else
        {
            var result = await GetDataFromNodesAsync(network.Nodes,
                async url => await new Web3(url).Eth.GetContractQueryHandler<BalanceOfFunction>()
                    .QueryAsync<BigInteger>(currency.TokenContract, new() { Owner = request.Address }));


            balance = result;
        }

        var balanceResponse = new BalanceResponse
        {
            AmountInWei = balance.ToString(),
            Amount = Web3.Convert.FromWei(balance, currency.Decimals),
            Decimals = currency.Decimals,
        };

        return balanceResponse;
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(GetEventsAsync)}")]
    public override async Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request)
    {
        var result = new HTLCBlockEventResponse();

        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var nodes = network!.Nodes;

        if (!nodes.Any())
        {
            throw new ArgumentException($"Node is not configured on {request.NetworkName} network", nameof(nodes));
        }

        var solverAccount = network.ManagedAccounts
            .First(x => x.Type == AccountType.LP);

        var currencies = await networkRepository.GetTokensAsync();

        var contractAddresses = new List<string>();

        var htlcNativeContractAddress = network.Contracts
            .FirstOrDefault(c => c.Type == ContarctType.HTLCNativeContractAddress);

        if (htlcNativeContractAddress != null)
        {
            contractAddresses.Add(htlcNativeContractAddress.Address);
        }

        var htlcTokenContractAddress = network.Contracts
            .FirstOrDefault(c => c.Type == ContarctType.HTLCTokenContractAddress);

        if (htlcTokenContractAddress != null)
        {
            contractAddresses.Add(htlcTokenContractAddress.Address);
        }

        var filterInput = new NewFilterInput
        {
            FromBlock = new BlockParameter(request.FromBlock),
            ToBlock = new BlockParameter(request.ToBlock),
            Address = contractAddresses.ToArray(),
        };

        var logsResult = await GetDataFromNodesAsync(nodes,
            async url =>
                await new Web3(url).Eth.Filters.GetLogs
                    .SendRequestAsync(filterInput));

        foreach (var log in logsResult)
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

                if (FormatAddress(new() { Address = commitedEvent.Receiver })
                    != FormatAddress(new() { Address = solverAccount.Address }))
                {
                    continue;
                }

                var commitId = commitedEvent.Id.ToHex(prefix: true);

                var sourceCurrency = currencies
                    .FirstOrDefault(x =>
                        x.Asset == commitedEvent.SourceAsset
                        && x.Network.Name == request.NetworkName);

                if (sourceCurrency is null)
                {
                    continue;
                }

                var destinationCurrency = currencies
                    .FirstOrDefault(x =>
                        x.Asset == commitedEvent.DestinationAsset
                        && x.Network.Name == commitedEvent.DestinationChain);

                if (destinationCurrency is null)
                {
                    continue;
                }

                var message = new HTLCCommitEventMessage
                {
                    TxId = log.TransactionHash,
                    Id = commitId,
                    Amount = Web3.Convert.FromWei(commitedEvent.Amount, sourceCurrency.Decimals),
                    AmountInWei = commitedEvent.Amount.ToString(),
                    SourceAsset = sourceCurrency.Asset,
                    SenderAddress = commitedEvent.Sender,
                    SourceNetwork = request.NetworkName,
                    DestinationAddress = FormatAddress(new() { Address = commitedEvent.DestinationAddress }),
                    DestinationNetwork = destinationCurrency.Network.Name,
                    DestinationAsset = destinationCurrency.Asset,
                    TimeLock = (long)commitedEvent.Timelock,
                    ReceiverAddress = FormatAddress(new() { Address = solverAccount.Address }),
                    DestinationNetworkType = destinationCurrency.Network.Type,
                    SourceNetworkType = sourceCurrency.Network.Type
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
                    Id = commitId.ToHex().EnsureHexPrefix(),
                    HashLock = lockAddedEvent.Hashlock.ToHex().EnsureHexPrefix(),
                    TimeLock = (long)lockAddedEvent.Timelock,
                };

                result.HTLCLockEventMessages.Add(message);
            }
        }

        return result;
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(GetLastConfirmedBlockNumberAsync)}")]
    public override async Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new ArgumentException($"Node is not configured on {request.NetworkName} network", nameof(nodes));
        }

        var blockResult = await GetDataFromNodesAsync(nodes,
            async url =>
                await new Web3(url).Eth.Blocks.GetBlockWithTransactionsHashesByNumber
                    .SendRequestAsync(BlockParameter.CreateLatest()));

        return new()
        {
            BlockNumber = (ulong)blockResult.Number.Value,
            BlockHash = blockResult.BlockHash,
        };
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(GetSpenderAllowanceAsync)}")]
    public virtual async Task<decimal> GetSpenderAllowanceAsync(AllowanceRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new ArgumentException($"Node is not configured on {request.NetworkName} network", nameof(nodes));
        }

        var currency = network.Tokens.Single(x => x.Asset == request.Asset);

        if (!string.IsNullOrEmpty(currency.TokenContract))
        {
            var allowanceFunctionMessage = new AllowanceFunction
            {
                Owner = request.OwnerAddress,
                Spender = request.SpenderAddress,
            };

            var allowanceHandler = await GetDataFromNodesAsync(nodes,
                async url =>
                    await Task.FromResult(new Web3(url).Eth.GetContractQueryHandler<AllowanceFunction>()));

            var allowance =
                await allowanceHandler.QueryAsync<BigInteger>(currency.TokenContract, allowanceFunctionMessage);
            return Web3.Convert.FromWei(allowance, currency.Decimals);
        }

        return decimal.MaxValue;
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(ValidateAddLockSignatureAsync)}")]
    public override async Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var currency = network.Tokens.Single(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        var htlcContractAddress = currency.IsNative
            ? network.Contracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.Contracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        var signer = new Eip712TypedDataSigner();

        var typedData = GetAddLockMessageTypedDefinition(
            long.Parse(network.ChainId!),
            htlcContractAddress);

        var addLockMsg = new AddLockMessage
        {
            Hashlock = request.Hashlock.HexToByteArray(),
            Id = request.Id.HexToByteArray(),
            Timelock = (ulong)request.Timelock
        };

        var signature = EthECDSASignatureFactory.FromComponents(
            request.R.HexToByteArray(),
            request.S.HexToByteArray(),
            byte.Parse(request.V)
        );

        var sign = EthECDSASignature.CreateStringSignature(signature);

        var addressRecovered = signer.RecoverFromSignatureV4(addLockMsg, typedData, sign);

        return string.Equals(addressRecovered, request.SignerAddress, StringComparison.OrdinalIgnoreCase);
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(GetNextNonceAsync)}")]
    public override async Task<string> GetNextNonceAsync(NextNonceRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        request.Address = FormatAddress(new() { Address = request.Address });

        return await GetNextNonceAsync(network.Nodes, request.Address);
    }

    protected override async Task<string> GetCachedNonceAsync(
        NextNonceRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var nodes = network.Nodes;

        await using var distributedLock = await distributedLockFactory.CreateLockAsync(
            resource: RedisHelper.BuildLockKey(request.NetworkName, request.Address),
            retryTime: TimeSpan.FromSeconds(1),
            waitTime: TimeSpan.FromSeconds(20),
            expiryTime: TimeSpan.FromSeconds(25));

        if (!distributedLock.IsAcquired)
        {
            throw new Exception("Failed to acquire the lock");
        }

        var curentNonce = new BigInteger(-1);

        var currentNonceRedis = await cache.StringGetAsync(RedisHelper.BuildNonceKey(request.NetworkName, request.Address));

        if (currentNonceRedis != RedisValue.Null)
        {
            curentNonce = BigInteger.Parse(currentNonceRedis!);
        }

        var nonce = await GetDataFromNodesAsync(nodes,
            async url =>
                await new Web3(url).Eth.Transactions.GetTransactionCount
                    .SendRequestAsync(request.Address, BlockParameter.CreatePending()));

        if (nonce <= curentNonce)
        {
            curentNonce++;
            nonce = new HexBigInteger(curentNonce);
        }
        else
        {
            curentNonce = nonce;
        }

        await cache.StringSetAsync(RedisHelper.BuildNonceKey(request.NetworkName, request.Address),
            curentNonce.ToString(),
            expiry: TimeSpan.FromDays(7));

        return nonce.ToString();
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(PublishRawTransactionAsync)}")]
    public virtual async Task<string> PublishRawTransactionAsync(EVMPublishTransactionRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        try
        {
            var result = await GetDataFromNodesAsync(network.Nodes,
                async url => await new
                        EthSendRawTransaction(new Web3(url).Client)
                    .SendRequestAsync(request.SignedTransaction.RawTxn));

            return result;
        }
        catch (AggregateException e)
        {
            foreach (var innerEx in e.InnerExceptions)
            {
                if (innerEx is RpcResponseException exNonceTooLow
                    && _nonRetriableErrors.Any(x =>
                        exNonceTooLow.Message.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                {
                    Log.Warning($"Nonce too low in {request.NetworkName}. {request.FromAddress}. Message {exNonceTooLow.Message}");

                    // Assuming if nonce too low then the transaction is already confirmed on blockchain and we have to return txId
                    break;
                }

                if (innerEx is RpcResponseException exInsuffFunds && _insuficientFundsErrors.Any(x =>
                        exInsuffFunds.Message.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                {
                    throw new Exception(
                        $"Insufficient funds in {request.NetworkName}. {request.FromAddress}. Message {exInsuffFunds.Message}",
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
                        $"Send raw transaction failed due to error(s): {e.Message} {string.Join('\t', e.InnerExceptions.Select(c => c.Message))}",
                        innerEx);
                }
            }
        }

        return request.SignedTransaction.Hash.EnsureHexPrefix();
    }

    [Activity(name: $"{nameof(NetworkType.EVM)}{nameof(ComposeSignedRawTransactionAsync)}")]
    public virtual async Task<SignedTransaction> ComposeSignedRawTransactionAsync(EVMComposeTransactionRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var privateKeyResult = await privateKeyProvider.GetAsync(request.FromAddress);

        var account = new Account(privateKeyResult, BigInteger.Parse(network.ChainId!));

        var transactionInput = new TransactionInput
        {
            From = request.FromAddress,
            To = request.ToAddress,
            Nonce = BigInteger.Parse(request.Nonce).ToHexBigInteger(),
            Value = BigInteger.Parse(request.AmountInWei).ToHexBigInteger(),
            Data = request.CallData?.EnsureHexPrefix()
        };

        if (request.Fee?.LegacyFeeData is not null)
        {
            var gasPrice = BigInteger.Parse(request.Fee.LegacyFeeData.GasPriceInWei);
            transactionInput.GasPrice = gasPrice.ToHexBigInteger();
            transactionInput.Gas = BigInteger.Parse(request.Fee.LegacyFeeData.GasLimit).ToHexBigInteger();
        }
        else if (request.Fee?.Eip1559FeeData is not null)
        {
            var maxFeePerGas = BigInteger.Parse(request.Fee.Eip1559FeeData.MaxFeePerGasInWei);
            var maxPriorityFeePerGas = BigInteger.Parse(request.Fee.Eip1559FeeData.MaxPriorityFeeInWei);

            transactionInput.Gas = BigInteger.Parse(request.Fee.Eip1559FeeData.GasLimit).ToHexBigInteger();
            transactionInput.MaxFeePerGas = maxFeePerGas.ToHexBigInteger();
            transactionInput.MaxPriorityFeePerGas = maxPriorityFeePerGas.ToHexBigInteger();
            transactionInput.Type = new HexBigInteger((int)Nethereum.Model.TransactionType.EIP1559);
        }

        return SignTransaction(account, transactionInput);
    }

    private static async Task<string> GetNextNonceAsync(List<Node> nodes, string address)
    {
        var nonce = await GetDataFromNodesAsync(nodes,
            async url =>
                await new Web3(url).Eth.Transactions.GetTransactionCount
                    .SendRequestAsync(address, BlockParameter.CreatePending()));

        return nonce.ToString();
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

    private SignedTransaction SignTransaction(
       Account account,
       TransactionInput transaction)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));

        if (string.IsNullOrWhiteSpace(transaction.From))
            transaction.From = account.Address;

        else if (!transaction.From.IsTheSameAddress(account.Address))
            throw new Exception("Invalid account used for signing, does not match the transaction input");

        var chainId = account.ChainId;

        var nonce = transaction.Nonce;
        if (nonce == null)
            throw new ArgumentNullException(nameof(transaction), "Transaction nonce has not been set");

        var gasLimit = transaction.Gas;
        var value = transaction.Value ?? new HexBigInteger(0);

        if (chainId == null) throw new ArgumentException("ChainId required for TransactionType 0X02 EIP1559");

        if (transaction.Type != null && transaction.Type.Value == Nethereum.Model.TransactionTypeExtensions.AsByte(Nethereum.Model.TransactionType.EIP1559))
        {
            var maxPriorityFeePerGas = transaction.MaxPriorityFeePerGas.Value;
            var maxFeePerGas = transaction.MaxFeePerGas.Value;

            var transaction1559 = new Nethereum.Model.Transaction1559(
                chainId.Value,
                nonce,
                maxPriorityFeePerGas,
                maxFeePerGas,
                gasLimit,
                transaction.To,
                value,
                transaction.Data,
                transaction.AccessList.ToSignerAccessListItemArray());

            var transaction1559Signer = new Transaction1559Signer();

            var rawTxnHex = transaction1559Signer.SignTransaction(new EthECKey(account.PrivateKey), transaction1559);

            return new()
            {
                RawTxn = rawTxnHex,
                Hash = transaction1559.Hash.ToHex(),
            };
        }
        else
        {
            var transactionLegacy = new Nethereum.Model.LegacyTransactionChainId(
                transaction.To,
                value.Value,
                nonce,
                transaction.GasPrice.Value,
                gasLimit.Value,
                transaction.Data,
                chainId.Value);

            var signature = new EthECKey(account.PrivateKey.HexToByteArray(), true).SignAndCalculateV(transactionLegacy.RawHash, transactionLegacy.GetChainIdAsBigInteger());
            transactionLegacy.SetSignature(new Nethereum.Model.Signature() { R = signature.R, S = signature.S, V = signature.V });

            return new()
            {
                Hash = transactionLegacy.Hash.ToHex(),
                RawTxn = transactionLegacy.GetRLPEncoded().ToHex(),
            };
        }
    }
}
