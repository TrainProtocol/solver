using FluentResults;
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
using System.Numerics;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.EVM.FunctionMessages;
using Train.Solver.Core.Blockchain.EVM.Helpers;
using Train.Solver.Core.Blockchain.EVM.Models;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Blockchain.Redis;
using Train.Solver.Core.Blockchain.Services;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Secret;
using Train.Solver.Data;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchain.EVM;

public abstract class BaseEVMBlockchainService(
    SolverDbContext dbContext,
    IResilientNodeService resNodeService,
    IDistributedLockFactory distributedLockFactory,
    IDatabase cache,
    IPrivateKeyProvider privateKeyProvider) : BlochainServiceBase(dbContext), IEVMBlockchainService
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

    public override async Task<Result<PrepareTransactionResponse>> BuildTransactionAsync(string networkName, TransactionType transactionType, string args)
    {
        var network = await dbContext.Networks
            .Include(n => n.Tokens)
            .Include(n => n.DeployedContracts)
                    .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail(
                new NotFoundError(
                    $"Chain is not configured for {networkName} network"));
        }

        Result<PrepareTransactionResponse> result;

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
                return Result.Fail("Unsupported type");
        }

        return result;
    }

    public override string FormatAddress(string address) => address.ToLower();

    public override async Task<Result<string>> GenerateAddressAsync(string networkName)
    {
        var network = await dbContext.Networks
            .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail(
                new NotFoundError(
                    $"Chain is not configured for {networkName} network"));
        }


        var account = new Account(EthECKey.GenerateKey().GetPrivateKeyAsBytes().ToHex());
        var formattedAddress = FormatAddress(account.Address);
        await privateKeyProvider.SetAsync(formattedAddress, account.PrivateKey.EnsureHexPrefix());

        return Result.Ok(formattedAddress);
    }

    public override bool ValidateAddress(string address)
    {
        return AddressUtil.Current.IsValidEthereumAddressHexFormat(FormatAddress(address));
    }

    public override async Task<Result<BalanceResponse>> GetBalanceAsync(string networkName, string address, string asset)
    {
        var network = await dbContext.Networks
          .Include(x => x.Nodes)
          .Include(x => x.Tokens)
          .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Chain is not configured for {networkName} network"));
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

        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == asset.ToUpper());
        if (currency is null)
        {
            return Result.Fail(new BadRequestError($"Invalid currency"));
        }

        BigInteger balance;

        if (currency.TokenContract is null)
        {
            try
            {
                var result = await resNodeService.GetDataFromNodesAsync(network.Nodes,
                    async url =>
                   await new Web3(url).Eth.GetBalance.SendRequestAsync(address));

                if (result.IsFailed)
                {
                    return Result.Fail(
                        new InternalError(
                            $"Failed to get balance of {currency.Asset} on {address} address in {networkName} network")
                            .CausedBy(result.Errors));
                }
                balance = result.Value;
                //(await web3.Eth.GetBalance.SendRequestAsync(address)).Value;
            }
            catch (Exception ex)
            {
                return Result.Fail(
                    new InternalError(
                        $"Failed to get balance of {currency.Asset} on {address} address in {networkName} network")
                        .CausedBy(ex));
            }
        }
        else
        {
            try
            {
                var result = await resNodeService.GetDataFromNodesAsync(network.Nodes,
                    async url => await new Web3(url).Eth.GetContractQueryHandler<BalanceOfFunction>()
                        .QueryAsync<BigInteger>(currency.TokenContract, new() { Owner = address }));

                if (result.IsFailed)
                {
                    return Result.Fail(
                        new InternalError(
                                $"Failed to get balance of {currency.Asset} on {address} address in {networkName} network")
                            .CausedBy(result.Errors));
                }

                balance = result.Value;
            }
            catch (Exception ex)
            {
                return Result.Fail(
                    new InternalError(
                        $"Failed to get balance of {currency.Asset} on {address} address in {networkName} network")
                        .CausedBy(ex));
            }
        }

        var balanceResponse = new BalanceResponse
        {
            AmountInWei = balance.ToString(),
            Amount = Web3.Convert.FromWei(balance, currency.Decimals),
            Decimals = currency.Decimals,
        };

        return Result.Ok(balanceResponse);
    }

    public override async Task<Result<TransactionReceiptModel>> GetConfirmedTransactionAsync(string networkName, string transactionId)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Include(x => x.ManagedAccounts)
            .FirstOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Chain is not configured on {networkName} network"));
        }

        var nodes = network!.Nodes;

        if (!nodes.Any())
        {
            return Result.Fail(
                  new NotFoundError(
                      $"Node is not configured on {networkName} network"));
        }

        var nativeCurrency = network.Tokens
            .SingleOrDefault(x => x.TokenContract is null);

        var tokenContracts = network.Tokens
            .Where(x => x.TokenContract is not null)
            .ToDictionary(key => FormatAddress(key.TokenContract!));

        var transaction = (await resNodeService.GetDataFromNodesAsync(nodes,
            async url =>
                await new Web3(url).Eth.Transactions.GetTransactionByHash.SendRequestAsync(transactionId)))
            .Value;

        if (transaction is null)
        {
            return Result.Fail(new TransactionNotFoundError($"Transaction not found on {networkName}"));
        }

        if (transaction.BlockNumber is null)
        {
            return Result.Fail(new TransactionNotConfirmedError($"Transaction not confirmed yet on {networkName}"));
        }

        if (string.IsNullOrEmpty(transaction.To))
        {
            return Result.Fail("Transaction recipient is missing");
        }

        var currentBlockNumber = (await resNodeService.GetDataFromNodesAsync(nodes,
            async url =>
                await new Web3(url).Eth.Blocks.GetBlockNumber.SendRequestAsync()))
            .Value;

        var transactionBlock = (await resNodeService.GetDataFromNodesAsync(nodes,
                async url =>
                    await new Web3(url).Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(transaction.BlockNumber)))
            .Value;

        if (transactionBlock == null || currentBlockNumber == null)
        {
            return Result.Fail(new InternalError($"Failed to retrieve block"));
        }

        var transactionReceiptResult = await resNodeService.GetTransactionReceiptAsync(nodes, transaction.TransactionHash);

        if (transactionReceiptResult.IsFailed)
        {
            return transactionReceiptResult.ToResult();
        }

        var networkFee = CalculateFee(
            transactionBlock,
            transaction,
            transactionReceiptResult.Value);

        try
        {
            var from = FormatAddress(transaction.From);
            var to = FormatAddress(transaction.To);

            var transactionModel = new TransactionReceiptModel
            {
                FeeAmountInWei = networkFee.ToString(),
                FeeAsset = nativeCurrency!.Asset,
                FeeDecimals = nativeCurrency.Decimals,
                FeeAmount = Web3.Convert.FromWei(networkFee, nativeCurrency.Decimals),
                TransactionId = transaction.TransactionHash,
                Timestamp = (long)transactionBlock.Timestamp.Value * 1000,
                Confirmations = (int)(currentBlockNumber.Value - transaction.BlockNumber.Value) + 1,
                BlockNumber = (long)transaction.BlockNumber.Value,
                Nonce = transaction.Nonce.Value.ToString(),
                Status = TransactionStatuses.Completed
            };

            return Result.Ok(transactionModel);
        }
        catch (Exception e)
        {
            return new FluentResults.Error("Failed to get transaction").CausedBy(e);
        }
    }

    public virtual BigInteger CalculateFee(Block block, Nethereum.RPC.Eth.DTOs.Transaction transaction, EVMTransactionReceiptModel receipt)
        =>
          transaction.Type is null || (byte)transaction.Type.Value != (byte)Nethereum.RPC.TransactionTypes.TransactionType.EIP1559
            ? receipt.GasUsed * transaction.GasPrice.Value
            : receipt.GasUsed * (block.BaseFeePerGas + transaction.MaxPriorityFeePerGas.Value);

    public override async Task<Result<HTLCBlockEvent>> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock)
    {
        var result = new HTLCBlockEvent();

        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.ManagedAccounts).Include(network => network.DeployedContracts)
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            return Result.Fail("Invalid network");
        }

        var nodes = network!.Nodes;

        if (!nodes.Any())
        {
            return Result.Fail(
                  new NotFoundError(
                      $"Node is not configured on {networkName} network"));
        }

        var solverAccount = network.ManagedAccounts
           .FirstOrDefault(x => x.Type == AccountType.LP);

        if (solverAccount is null)
        {
            return Result.Fail($"Solver address for network: {networkName} is not configured");
        }

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

        var logsResult = await resNodeService.GetDataFromNodesAsync(nodes,
           async url =>
               await new Web3(url).Eth.Filters.GetLogs
                   .SendRequestAsync(filterInput));

        if (logsResult.IsFailed)
        {
            return Result.Fail("Failed to get block number");
        }

        foreach (var log in logsResult.Value)
        {
            var decodedEvent = EventDecoder.Decode(log);

            if (decodedEvent == null)
            {
                continue;
            }

            var (eventType, typedEvent) = decodedEvent.Value;

            if (eventType == typeof(EtherTokenCommittedEventDTO) || decodedEvent.Value.eventType == typeof(ERC20TokenCommitedEventDTO))
            {
                var commitedEvent = (EtherTokenCommittedEventDTO)typedEvent;

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
                    DestinationAddress = commitedEvent.DestinationAddress,
                    DestinationNetwork = destinationCurrency.Network.Name,
                    DestinationAsset = destinationCurrency.Asset,
                    TimeLock = (long)commitedEvent.Timelock,
                    ReceiverAddress = solverAccount.Address,
                };

                result.HTLCCommitEventMessages.Add(message);
            }
            else if (eventType == typeof(EtherTokenLockAddedDTO))
            {
                var lockAddedEvent = (EtherTokenLockAddedDTO)typedEvent;
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

    public override async Task<Result<BlockNumberResponse>> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            return Result.Fail("Invalid network");
        }

        var nodes = network!.Nodes;

        if (!nodes.Any())
        {
            return Result.Fail(
                  new NotFoundError(
                      $"Node is not configured on {networkName} network"));
        }

        var blockResult = await resNodeService.GetDataFromNodesAsync(nodes,
            async url =>
                await new Web3(url).Eth.Blocks.GetBlockWithTransactionsHashesByNumber
                    .SendRequestAsync(Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest()));

        if (blockResult.IsFailed)
        {
            return Result.Fail("Failed to get block number");
        }

        return Result.Ok(new BlockNumberResponse
        {
            BlockNumber = blockResult.Value.Number.Value.ToString(),
            BlockHash = blockResult.Value.BlockHash,
        });
    }

    public override async Task<Result<string>> GetSpenderAllowanceAsync(string networkName, string ownerAddress, string spenderAddress, string asset)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Chain is not configured for {networkName} network"));
        }

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Primary node is not configured on {networkName} network"));
        }

        var currency = network.Tokens.SingleOrDefault(x => x.Asset == asset);

        if (currency is null)
        {
            return Result.Fail(
                    new NotFoundError(
                        $"Currency is not configured on {networkName} network"));
        }

        if (!string.IsNullOrEmpty(currency.TokenContract))
        {
            var allowanceFunctionMessage = new AllowanceFunction
            {
                Owner = ownerAddress,
                Spender = spenderAddress,
            };

            var allowanceHandler = (await resNodeService.GetDataFromNodesAsync(nodes,
                async url =>
                    await Task.FromResult(new Web3(url).Eth.GetContractQueryHandler<AllowanceFunction>())))
                .Value;

            var allowance = await allowanceHandler.QueryAsync<BigInteger>(currency.TokenContract, allowanceFunctionMessage);
            return Result.Ok(allowance.ToString());
        }
        else
        {
            return Result.Ok(UInt128.MaxValue.ToString());
        }
    }

    public override async Task<Result<bool>> ValidateAddLockSignatureAsync(string networkName, AddLockSigValidateRequest request)
    {
        var network = await dbContext.Networks
                    .Include(x => x.Tokens).Include(network => network.DeployedContracts)
                    .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network == null)
        {
            return Result.Fail($"Network with name: {networkName} is not configured");
        }


        var currency = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == request.Asset.ToUpper());


        if (currency is null)
        {
            return Result.Fail($"Currency {request.Asset} for {networkName} is missing");
        }

        var htlcContractAddress = currency.IsNative
            ? network.DeployedContracts.First(c => c.Type == ContarctType.HTLCNativeContractAddress).Address
            : network.DeployedContracts.First(c => c.Type == ContarctType.HTLCTokenContractAddress).Address;

        try
        {
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
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public override async Task<Result<string>> GetNextNonceAsync(
        string networkName,
        string address,
        string referenceId)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .SingleOrDefaultAsync(x => x.Name == networkName);

        if (network == null)
        {
            return Result.Fail($"Network {networkName} is not configured");
        }

        var sourceAddress = FormatAddress(address);

        var nodes = network.Nodes;

        await using var distributedLock = await distributedLockFactory.CreateLockAsync(
            resource: RedisHelper.BuildLockKey(networkName, sourceAddress),
            retryTime: TimeSpan.FromSeconds(1),
            waitTime: TimeSpan.FromSeconds(20),
            expiryTime: TimeSpan.FromSeconds(25));

        if (!distributedLock.IsAcquired)
        {
            return new InfinitlyRetryableError(seconds: 30).CausedBy("Failed to acquire the lock");
        }

        try
        {
            var curentNonce = new BigInteger(-1);

            var currentNonceRedis = await cache.StringGetAsync(RedisHelper.BuildNonceKey(networkName, sourceAddress));

            if (currentNonceRedis != RedisValue.Null)
            {
                curentNonce = BigInteger.Parse(currentNonceRedis!);
            }

            var nonce = (await resNodeService.GetDataFromNodesAsync(nodes,
                async url =>
                    await new Web3(url).Eth.Transactions.GetTransactionCount
                        .SendRequestAsync(sourceAddress, BlockParameter.CreatePending())))
                .Value;

            if (nonce.Value <= curentNonce)
            {
                curentNonce++;
                nonce = new HexBigInteger(curentNonce);
            }
            else
            {
                curentNonce = nonce.Value;
            }

            await cache.StringSetAsync(RedisHelper.BuildNonceKey(networkName, sourceAddress),
                curentNonce.ToString(),
                expiry: TimeSpan.FromDays(7));

            return nonce.ToString();
        }
        catch (Exception e)
        {
            return new InfinitlyRetryableError().CausedBy("Failed to retrieve nonce", e);
        }
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

    public virtual async Task<Result<string>> PublishRawTransactionAsync(
        string networkName,
        string fromAddress,
        SignedTransaction signedTransaction)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Where(x => x.Name.ToUpper() == networkName.ToUpper())
            .FirstOrDefaultAsync();

        if (network == null)
        {
            return Result.Fail("Invalid network");
        }

        try
        {
            var result = await resNodeService.GetDataFromNodesAsync(network.Nodes,
                async url => await new
                        EthSendRawTransaction(new Web3(url).Client)
                    .SendRequestAsync(signedTransaction.RawTxn));

            if (result.IsSuccess)
            {
                return result.Value;
            }
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
                    return Result.Fail(
                        new InsuficientFundsForGasLimitError($"Insufficient funds in {networkName}. {fromAddress}. Message {exInsuffFunds.Message}"));
                }

                if (innerEx is Exception exReplacement
                    && _replacementErrors.Any(x =>
                        exReplacement.Message.Contains(x, StringComparison.CurrentCultureIgnoreCase)))
                {
                    return Result.Fail(new TransactionUnderpricedError());
                }

                if (innerEx is Exception exGeneral)
                {
                    return Result.Fail($"Send raw transaction failed due to error(s): {exGeneral.Message}, Raw transaction: {signedTransaction.RawTxn}");
                }
            }
        }
        catch (Exception e)
        {
            return Result.Fail($"Exception happened. Message: {e.Message}.");
        }

        return signedTransaction.Hash.EnsureHexPrefix();
    }

    public async Task<Result<SignedTransaction>> ComposeSignedRawTransactionAsync(
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
            .FirstOrDefaultAsync();

        if (network == null)
        {
            return Result.Fail("Invalid network");
        }

        var privateKeyResult = await privateKeyProvider.GetAsync(fromAddress);

        if (privateKeyResult.IsFailed)
        {
            return Result.Fail("Private key not found");
        }

        var account = new Account(privateKeyResult.Value, BigInteger.Parse(network.ChainId!));

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

        var signedTransaction = new EVMTransactionSigner().SignTransaction(account, transactionInput);

        return Result.Ok(
            new SignedTransaction()
            {
                Hash = signedTransaction.Hash.EnsureHexPrefix(),
                RawTxn = signedTransaction.SignedTxn.EnsureHexPrefix()
            });
    }

    public abstract Result<Fee> IncreaseFee(
        Fee requestFee,
        int feeIncreasePercentage);

    public abstract Fee MaxFee(Fee currentFee, Fee increasedFee);

}

