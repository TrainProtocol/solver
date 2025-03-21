using System.Numerics;
using Microsoft.EntityFrameworkCore;
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
using Train.Solver.Data;
using Train.Solver.Data.Entities;
using static Train.Solver.Core.Helpers.ResilientNodeHelper;
using static Train.Solver.Core.Blockchains.EVM.Helpers.EVMResilientNodeHelper;
using Train.Solver.Core.Helpers;
using Train.Solver.Core.Models;
using Train.Solver.Core.Blockchains.EVM.FunctionMessages;
using Train.Solver.Core.Blockchains.EVM.Helpers;
using Train.Solver.Core.Blockchains.EVM.Models;
using Train.Solver.Core.Services.Secret;
using Train.Solver.Core.Activities;
using Train.Solver.Core.Blockchains.EVM.Activities;
using Nethereum.RPC.Eth.Mappers;

namespace Train.Solver.Core.Blockchains.EVM.Services;

public abstract class EVMBlockchainActivitiesBase(
    SolverDbContext dbContext,
    IDistributedLockFactory distributedLockFactory,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) : BlockchainActivitiesBase(dbContext), IEVMBlockchainActivities
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

    public abstract Fee IncreaseFee(Fee requestFee, int feeIncreasePercentage);

    public override async Task<TransactionModel> GetTransactionAsync(string networkName, string transactionId)
    {
        var network = await dbContext.Networks
           .Include(x => x.Nodes)
           .Include(x => x.Tokens)
           .Where(x => x.Name.ToUpper() == networkName)
           .SingleAsync();

        var transaction = await GetTransactionAsync(network, transactionId);

        if (transaction == null)
        {
            throw new TransactionNotComfirmedException("Transaction not found");
        }

        return transaction;
    }

    public virtual async Task<TransactionModel> GetBatchTransactionAsync(string networkName, string[] transactionIds)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Where(x => x.Name.ToUpper() == networkName)
            .SingleAsync();

        TransactionModel? transaction = null;

        foreach (var transactionId in transactionIds)
        {
            transaction = await GetTransactionAsync(network, transactionId);
        }

        if (transaction == null)
        {
            throw new TransactionNotComfirmedException("Transaction not confirmed");
        }

        return transaction;
    }

    private async Task<TransactionModel> GetTransactionAsync(Network network, string transactionId)
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

        var networkFee = CalculateFee(
            transactionBlock,
            transaction,
            transactionReceiptResult);

        var from = FormatAddress(transaction.From);
        var to = FormatAddress(transaction.To);

        var transactionModel = new TransactionModel
        {
            NetworkName = network.Name,
            Status = TransactionStatus.Completed,
            TransactionHash = transaction.TransactionHash,
            FeeAmount = Web3.Convert.FromWei(networkFee, nativeCurrency.Decimals),
            FeeAsset = nativeCurrency!.Asset,
            Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)transactionBlock.Timestamp.Value * 1000),
            Confirmations = (int)(currentBlockNumber.Value - transaction.BlockNumber.Value) + 1
        };

        return transactionModel;
    }

    public override async Task<PrepareTransactionResponse> BuildTransactionAsync(string networkName,
        TransactionType transactionType, string args)
    {
        var network = await dbContext.Networks
            .Include(n => n.Tokens)
            .Include(n => n.DeployedContracts)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        PrepareTransactionResponse result;

        switch (transactionType)
        {
            case TransactionType.Transfer:
                result = EVMTransactionBuilder.BuildTransferTransaction(network, args);
                break;
            case TransactionType.Approve:
                result = EVMTransactionBuilder.BuildApproveTransaction(network, args);

                break;
            case TransactionType.HTLCCommit:
                result = EVMTransactionBuilder.BuildHTLCCommitTransaction(network, args);

                break;
            case TransactionType.HTLCLock:
                result = EVMTransactionBuilder.BuildHTLCLockTransaction(network, args);

                break;
            case TransactionType.HTLCRedeem:
                result = EVMTransactionBuilder.BuildHTLCRedeemTranaction(network, args);

                break;
            case TransactionType.HTLCRefund:
                result = EVMTransactionBuilder.BuildHTLCRefundTransaction(network, args);

                break;
            case TransactionType.HTLCAddLockSig:
                result = EVMTransactionBuilder.BuildHTLCAddLockSigTransaction(network, args);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(transactionType),
                    $"Transaction type {transactionType} is not supported for network {network.Name}");
        }

        return result;
    }

    public override string FormatAddress(string address) => address.ToLower();

    public override async Task<string> GenerateAddressAsync(string networkName)
    {
        var network = await dbContext.Networks
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var account = new Account(EthECKey.GenerateKey().GetPrivateKeyAsBytes().ToHex());
        var formattedAddress = FormatAddress(account.Address);
        await privateKeyProvider.SetAsync(formattedAddress, account.PrivateKey.EnsureHexPrefix());

        return formattedAddress;
    }

    public override bool ValidateAddress(string address)
    {
        return AddressUtil.Current.IsValidEthereumAddressHexFormat(FormatAddress(address));
    }

    public override async Task<BalanceModel> GetBalanceAsync(string networkName, string address, string asset)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            throw new ArgumentException($"Chain is not configured for {networkName} network");
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

        var currency = network.Tokens.Single(x => x.Asset.ToUpper() == asset.ToUpper());

        BigInteger balance;

        if (currency.TokenContract is null)
        {
            var result = await GetDataFromNodesAsync(network.Nodes,
                async url =>
                    await new Web3(url).Eth.GetBalance.SendRequestAsync(address));

            balance = result.Value;
            //(await web3.Eth.GetBalance.SendRequestAsync(address)).Value;
        }
        else
        {
            var result = await GetDataFromNodesAsync(network.Nodes,
                async url => await new Web3(url).Eth.GetContractQueryHandler<BalanceOfFunction>()
                    .QueryAsync<BigInteger>(currency.TokenContract, new() { Owner = address }));


            balance = result;
        }

        var balanceResponse = new BalanceModel
        {
            AmountInWei = balance.ToString(),
            Amount = Web3.Convert.FromWei(balance, currency.Decimals),
            Decimals = currency.Decimals,
        };

        return balanceResponse;
    }

    protected virtual BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction,
        EVMTransactionReceipt receipt)
        =>
            transaction.Type is null || (byte)transaction.Type.Value !=
            (byte)Nethereum.RPC.TransactionTypes.TransactionType.EIP1559
                ? receipt.GasUsed * transaction.GasPrice.Value
                : receipt.GasUsed * (block.BaseFeePerGas + transaction.MaxPriorityFeePerGas.Value);

    public override async Task<HTLCBlockEvent> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock)
    {
        var result = new HTLCBlockEvent();

        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.ManagedAccounts).Include(network => network.DeployedContracts)
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .SingleAsync();

        var nodes = network!.Nodes;

        if (!nodes.Any())
        {
            throw new ArgumentException($"Node is not configured on {networkName} network", nameof(nodes));
        }

        var solverAccount = network.ManagedAccounts
            .First(x => x.Type == AccountType.LP);

        var currencies = await dbContext.Tokens
            .Include(x => x.Network)
            .ToListAsync();

        var contractAddresses = new List<string>();

        var htlcNativeContractAddress = network.DeployedContracts
            .FirstOrDefault(c => c.Type == ContarctType.HTLCNativeContractAddress);

        if (htlcNativeContractAddress != null)
        {
            contractAddresses.Add(htlcNativeContractAddress.Address);
        }

        var htlcTokenContractAddress = network.DeployedContracts
            .FirstOrDefault(c => c.Type == ContarctType.HTLCTokenContractAddress);

        if (htlcTokenContractAddress != null)
        {
            contractAddresses.Add(htlcTokenContractAddress.Address);
        }

        var filterInput = new NewFilterInput
        {
            FromBlock = new BlockParameter(fromBlock),
            ToBlock = new BlockParameter(toBlock),
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

                if (FormatAddress(commitedEvent.Receiver)
                    != FormatAddress(solverAccount.Address))
                {
                    continue;
                }

                var commitId = commitedEvent.Id.ToHex(prefix: true);

                var sourceCurrency = currencies
                    .FirstOrDefault(x =>
                        x.Asset == commitedEvent.SourceAsset
                        && x.Network.Name == networkName);

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
                    SourceNetwork = networkName,
                    DestinationAddress = FormatAddress(commitedEvent.DestinationAddress),
                    DestinationNetwork = destinationCurrency.Network.Name,
                    DestinationAsset = destinationCurrency.Asset,
                    TimeLock = (long)commitedEvent.Timelock,
                    ReceiverAddress = FormatAddress(solverAccount.Address),
                    DestinationNetwrokGroup = destinationCurrency.Network.Group,
                    SourceNetwrokGroup = sourceCurrency.Network.Group
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

    public override async Task<BlockNumberModel> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .SingleAsync();

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new ArgumentException($"Node is not configured on {networkName} network", nameof(nodes));
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

    public virtual async Task<decimal> GetSpenderAllowanceAsync(string networkName, string ownerAddress,
        string spenderAddress, string asset)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new ArgumentException($"Node is not configured on {networkName} network", nameof(nodes));
        }

        var currency = network.Tokens.Single(x => x.Asset == asset);

        if (!string.IsNullOrEmpty(currency.TokenContract))
        {
            var allowanceFunctionMessage = new AllowanceFunction
            {
                Owner = ownerAddress,
                Spender = spenderAddress,
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

    public override async Task<bool> ValidateAddLockSignatureAsync(string networkName,
        AddLockSignatureRequest request)
    {
        var network = await dbContext.Networks
            .Include(x => x.Tokens).Include(network => network.DeployedContracts)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var currency = network.Tokens.Single(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        var htlcContractAddress = currency.IsNative
            ? network.DeployedContracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

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

    public override async Task<string> GetNextNonceAsync(string networkName, string address)
    {
        var network = await dbContext.Networks
          .Include(x => x.Nodes)
          .SingleAsync(x => x.Name == networkName);

        var sourceAddress = FormatAddress(address);

        return await GetNextNonceAsync(network.Nodes, sourceAddress);
    }    

    protected override async Task<string> GetPersistedNonceAsync(
        string networkName,
        string address)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .SingleAsync(x => x.Name == networkName);

        var sourceAddress = FormatAddress(address);

        await using var distributedLock = await distributedLockFactory.CreateLockAsync(
            resource: RedisHelper.BuildLockKey(networkName, sourceAddress),
            retryTime: TimeSpan.FromSeconds(1),
            waitTime: TimeSpan.FromSeconds(20),
            expiryTime: TimeSpan.FromSeconds(25));

        if (!distributedLock.IsAcquired)
        {
            throw new Exception("Failed to acquire the lock");
        }

        var curentNonce = new BigInteger(-1);

        var currentNonceRedis = await cache.StringGetAsync(RedisHelper.BuildNonceKey(networkName, sourceAddress));

        if (currentNonceRedis != RedisValue.Null)
        {
            curentNonce = BigInteger.Parse(currentNonceRedis!);
        }

        var nodes = network.Nodes;

        var nonce = BigInteger.Parse(await GetNextNonceAsync(nodes, sourceAddress));

        if (nonce <= curentNonce)
        {
            curentNonce++;
            nonce = new HexBigInteger(curentNonce);
        }
        else
        {
            curentNonce = nonce;
        }

        await cache.StringSetAsync(RedisHelper.BuildNonceKey(networkName, sourceAddress),
            curentNonce.ToString(),
            expiry: TimeSpan.FromDays(7));

        return nonce.ToString();
    }
    
    public virtual async Task<string> PublishRawTransactionAsync(
        string networkName,
        string fromAddress,
        SignedTransaction signedTransaction)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .SingleAsync();

        try
        {
            var result = await GetDataFromNodesAsync(network.Nodes,
                async url => await new
                        EthSendRawTransaction(new Web3(url).Client)
                    .SendRequestAsync(signedTransaction.RawTxn));

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
                    Log.Warning($"Nonce too low in {networkName}. {fromAddress}. Message {exNonceTooLow.Message}");

                    // Assuming if nonce too low then the transaction is already confirmed on blockchain and we have to return txId
                    break;
                }

                if (innerEx is RpcResponseException exInsuffFunds && _insuficientFundsErrors.Any(x =>
                        exInsuffFunds.Message.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                {
                    throw new Exception(
                        $"Insufficient funds in {networkName}. {fromAddress}. Message {exInsuffFunds.Message}",
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

        return signedTransaction.Hash.EnsureHexPrefix();
    }

    public virtual async Task<SignedTransaction> ComposeSignedRawTransactionAsync(
        string networkName,
        string fromAddress,
        string toAddress,
        string nonce,
        string amountInWei,
        string? callData,
        Fee fee)
    {
        var network = await dbContext.Networks
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .SingleAsync();

        var privateKeyResult = await privateKeyProvider.GetAsync(fromAddress);

        var account = new Account(privateKeyResult, BigInteger.Parse(network.ChainId!));

        var transactionInput = new TransactionInput
        {
            From = fromAddress,
            To = toAddress,
            Nonce = BigInteger.Parse(nonce).ToHexBigInteger(),
            Value = BigInteger.Parse(amountInWei).ToHexBigInteger(),
            Data = callData?.EnsureHexPrefix()
        };

        if (fee?.LegacyFeeData is not null)
        {
            var gasPrice = BigInteger.Parse(fee.LegacyFeeData.GasPriceInWei);
            transactionInput.GasPrice = gasPrice.ToHexBigInteger();
            transactionInput.Gas = BigInteger.Parse(fee.LegacyFeeData.GasLimit).ToHexBigInteger();
        }
        else if (fee?.Eip1559FeeData is not null)
        {
            var maxFeePerGas = BigInteger.Parse(fee.Eip1559FeeData.MaxFeePerGasInWei);
            var maxPriorityFeePerGas = BigInteger.Parse(fee.Eip1559FeeData.MaxPriorityFeeInWei);

            transactionInput.Gas = BigInteger.Parse(fee.Eip1559FeeData.GasLimit).ToHexBigInteger();
            transactionInput.MaxFeePerGas = maxFeePerGas.ToHexBigInteger();
            transactionInput.MaxPriorityFeePerGas = maxPriorityFeePerGas.ToHexBigInteger();
            transactionInput.Type = new HexBigInteger((int)Nethereum.Model.TransactionType.EIP1559);
        }

        return SignTransaction(account, transactionInput);
    }

    private Models.SignedTransaction SignTransaction(
       Nethereum.Web3.Accounts.Account account,
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

    private static async Task<string> GetNextNonceAsync(List<Node> nodes, string address)
    {
        var nonce = await GetDataFromNodesAsync(nodes,
            async url =>
                await new Web3(url).Eth.Transactions.GetTransactionCount
                    .SendRequestAsync(address, BlockParameter.CreatePending()));

        return nonce.ToString();
    }

    // Todo
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

}
