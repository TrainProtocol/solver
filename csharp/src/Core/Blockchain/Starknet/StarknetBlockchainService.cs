using FluentResults;
using Microsoft.EntityFrameworkCore;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Newtonsoft.Json;
using RedLockNet;
using StackExchange.Redis;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Train.Solver.Core.Blockchain.Abstractions;
using Train.Solver.Core.Blockchain.Models;
using Train.Solver.Core.Blockchain.Redis;
using Train.Solver.Core.Blockchain.Services;
using Train.Solver.Core.Blockchain.Starknet.Extensions;
using Train.Solver.Core.Blockchain.Starknet.Helpers;
using Train.Solver.Core.Blockchain.Starknet.Models;
using Train.Solver.Core.Errors;
using Train.Solver.Core.Extensions;
using Train.Solver.Data;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchain.Starknet;

public class StarknetBlockchainService(
    SolverDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IResilientNodeService resNodeService,
    IDatabase cache,
    IDistributedLockFactory distributedLockFactory) : BlochainServiceBase(dbContext), IStarknetBlockchainService
{
    public static NetworkGroup NetworkGroup => NetworkGroup.STARKNET;
    private static readonly BigInteger BigIntTwo = new BigInteger(2);
    private static readonly BigInteger Mask221 = BigInteger.Pow(BigIntTwo, 221);
    private static readonly BigInteger Mask251 = BigInteger.Pow(BigIntTwo, 251);

    public override Task<Result<PrepareTransactionResponse>> BuildTransactionAsync(string networkName, TransactionType transactionType, string args)
    {
        throw new NotImplementedException();
    }

    public override Task<Result<Fee>> EstimateFeeAsync(string network, string asset, string fromAddress, string toAddress, decimal amount, string? data = null)
    {
        throw new NotImplementedException();
    }

    public override string FormatAddress(string address) => address.AddAddressPadding().ToLowerInvariant();

    public override Task<Result<string>> GenerateAddressAsync(string networkName)
    {
        throw new NotImplementedException();
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

        var token = network.Tokens.SingleOrDefault(x => x.Asset.ToUpper() == asset.ToUpper());

        if (token is null)
        {
            return Result.Fail(new BadRequestError($"Invalid currency"));
        }

        if (token.TokenContract is null)
        {
            return Result.Fail(new BadRequestError($"Token contract is not configured for {asset}"));
        }

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            return Result.Fail(new BadRequestError($"Nodes are not configured for {networkName} network"));
        }

        try
        {
            var requestJson = BuildRequestPayload(address, token);

            var result = (await resNodeService
                .GetDataFromNodesAsync(nodes,
                    async url => await GetBalance(url, requestJson))).Value;

            return new BalanceResponse
            {
                AmountInWei = result.ToString(),
                Amount = Web3.Convert.FromWei(result, token.Decimals),
                Decimals = token.Decimals,
            };
        }
        catch (Exception ex)
        {
            return Result.Fail(
                new InternalError(
                        $"Failed to get balance of {token.Asset} on {address} address in {networkName} network")
                    .CausedBy(ex));
        }
    }

    private static string BuildRequestPayload(string address, Token token)
    {
        var requestPayload = new
        {
            jsonrpc = "2.0",
            id = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            method = StarknetConstants.RpcMethods.StarknetCall,
            @params = new
            {
                request = new
                {
                    contract_address = token.TokenContract,
                    entry_point_selector = "0x2e4263afad30923c891518314c3c95dbe830a16874e8abc5777a9a20b54c76e",
                    calldata = new[] { address }
                },
                block_id = "latest"
            }
        };
        return JsonConvert.SerializeObject(requestPayload);
    }

    private async Task<BigInteger> GetBalance(string url, string requestJson)
    {
        var client = httpClientFactory.CreateClient();
        var result = await (await client
                .PostAsync(url, new StringContent(requestJson, Encoding.UTF8, "application/json"))).Content
            .ReadAsStringAsync();

        var starknetBalanceResponse = JsonConvert.DeserializeObject<GetBalanceResponse>(result);

        var balance = starknetBalanceResponse.Result[0].Value;
        return balance;
    }

    public override async Task<Result<TransactionReceiptModel>> GetConfirmedTransactionAsync(string networkName, string transactionId)
    {
        var network = await dbContext.Networks
               .Include(x => x.Tokens)
               .Include(x => x.Nodes)
               .Include(x => x.ManagedAccounts)
               .Include(x => x.DeployedContracts)
               .SingleOrDefaultAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail($"Network: {networkName} is not configured");
        }

        var node = network.Nodes.SingleOrDefault(x => x.Type == NodeType.DepositTracking);

        if (node is null)
        {
            return Result.Fail($"Node with type: {NodeType.DepositTracking} is not configured in {networkName}");
        }

        var rpcClient = new StarknetRpcClient(new Uri(node.Url));

        var managedAccountAddress = FormatAddress(network.ManagedAccounts.Single(x => x.Type == AccountType.LP).Address);

        var watchdogContract = network.DeployedContracts.FirstOrDefault(x => x.Type == ContarctType.WatchdogContractAddress);

        if (watchdogContract is null)
        {
            return Result.Fail($"Watchdog contract address is not configured in {networkName}");
        }

        var watchdogContractAddress = FormatAddress(watchdogContract.Address);

        TransactionReceiptModel? transactionModel = null;

        try
        {
            var transactionStatusResponse = await rpcClient.
                SendRequestAsync<StatusResponse>(new RpcRequest(
                        id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        method: StarknetConstants.RpcMethods.GetTransactionStatus,
                        transactionId!));

            var statusResult = ValidateTransactionStatus(transactionStatusResponse.FinalityStatus, transactionStatusResponse.ExecutionStatus);

            if (statusResult.IsFailed)
            {
                return Result.Fail("Unknown status");
            }
            else if (statusResult.IsSuccess && statusResult.Value == TransactionStatuses.Failed)
            {
                return Result.Fail(new TransactionFailedError($"Transaction receipt in {networkName} indicates failure"));
            }
            else
            {
                var transactionReceiptResponse = await rpcClient
                     .SendRequestAsync<TransactionResponse>(
                         new RpcRequest(
                            id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            method: StarknetConstants.RpcMethods.GetReceipt,
                            transactionId!));

                var feeInWei = new HexBigInteger(transactionReceiptResponse.ActualFee.Amount).Value;
                var feeDecimals = 18;

                transactionModel = new TransactionReceiptModel
                {
                    TransactionId = FormatAddress(transactionReceiptResponse.TransactionHash),
                    Confirmations = statusResult.Value == TransactionStatuses.Pending ? 0 : 1,
                    BlockNumber = transactionReceiptResponse.BlockNumber,
                    Status = statusResult.Value,
                    FeeAsset = "ETH",
                    FeeDecimals = feeDecimals,
                    FeeAmount = Web3.Convert.FromWei(feeInWei, feeDecimals),
                    FeeAmountInWei = feeInWei.ToString(),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                };

                if (transactionReceiptResponse.BlockNumber is not null)
                {
                    var getBlockResponse = await rpcClient
                        .SendRequestAsync<GetBlockResponse>(
                            new RpcRequest(
                               id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                               method: StarknetConstants.RpcMethods.GetBlockWithHashes,
                               parameterList: new GetBlockRequest
                               {
                                   BlockNumber = transactionReceiptResponse.BlockNumber.Value
                               }));

                    transactionModel.Timestamp = getBlockResponse.Timestamp * 1000;
                }

                return transactionModel;
            }
        }
        catch (Exception e)
        {
            var error = $"Failed to retrieve transaction receipt for detected transaction {{TransactionId}} in network: {networkName}. The transaction will be processed during the next run. Reason {e.Message}";
            return Result.Fail(error);
        }
    }

    public override async Task<Result<HTLCBlockEvent>> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock)
    {
        var network = await dbContext.Networks
             .Include(x => x.Nodes)
             .Include(x => x.Tokens)
             .Include(x => x.ManagedAccounts)
             .Include(x => x.DeployedContracts)
             .FirstOrDefaultAsync(x =>
                x.Name.ToUpper() == networkName.ToUpper());

        if (network is null)
        {
            return Result.Fail($"Chain for network: {networkName} is not configured");
        }

        var node = network!.Nodes.FirstOrDefault(x => x.Type == NodeType.DepositTracking);

        if (node is null)
        {
            return Result.Fail($"Node for network: {networkName} is not configured");
        }

        var htlcTokenContract = network.DeployedContracts
            .FirstOrDefault(x => x.Type == ContarctType.HTLCTokenContractAddress);

        if (htlcTokenContract is null)
        {
            return Result.Fail($"HTLC Token contract for network: {networkName} is not configured");
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

        var rpcClient = new StarknetRpcClient(new Uri(node.Url));

        return await TrackBlockEventsAsync(
            network.Name,
            rpcClient,
            currencies,
            solverAccount.Address,
            fromBlock: (int)fromBlock,
            toBlock: (int)toBlock,
            htlcTokenContract.Address);
    }

    private async Task<Result<HTLCBlockEvent>> TrackBlockEventsAsync(
       string networkName,
       IClient web3Client,
       List<Token> currencies,
       string solverAddress,
       int fromBlock,
       int toBlock,
       string? htlcContractAddress = null)
    {
        var getEventsRequest = new GetEventsRequest<Block>
        {
            Address = htlcContractAddress!,
            FromBlock = new Block
            {
                BlockNumber = fromBlock,
            },
            ToBlock = new Block
            {
                BlockNumber = toBlock,
            }
        };

        var eventsResponse = await web3Client
            .SendRequestAsync<GetEventsResponse>(
                new RpcRequest(
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    StarknetConstants.RpcMethods.GetEvents,
                    getEventsRequest));

        var result = new HTLCBlockEvent();

        foreach (var htlcEvent in eventsResponse.Events)
        {
            try
            {
                if (htlcEvent.Keys.Contains(StarknetConstants.EventIds.HTLCLockEventId))
                {
                    var deserializedTokenLockedEventResult = htlcEvent.DeserializeLockEventData();

                    if (deserializedTokenLockedEventResult.IsFailed)
                    {
                        return Result.Fail(deserializedTokenLockedEventResult.Errors.First().Message);
                    }

                    var lockMessage = new HTLCLockEventMessage
                    {
                        TxId = htlcEvent.TransactionHash,
                        HashLock = deserializedTokenLockedEventResult.Value.Hashlock.ToHexBigInteger().HexValue.ToString(),
                        Id = deserializedTokenLockedEventResult.Value.Id.ToHexBigInteger().HexValue.ToString(),
                        TimeLock = (long)deserializedTokenLockedEventResult.Value.Timelock,
                    };

                    result.HTLCLockEventMessages.Add(lockMessage);
                }
                else if (htlcEvent.Keys.Contains(StarknetConstants.EventIds.HTLCCommitEventId))
                {
                    var starknetTokenCommittedEventResult = htlcEvent.DeserializeCommitEventData();

                    if (starknetTokenCommittedEventResult.IsFailed)
                    {
                        return Result.Fail(starknetTokenCommittedEventResult.Errors.First().Message);
                    }

                    if (FormatAddress(starknetTokenCommittedEventResult.Value.SourceReciever)
                        != FormatAddress(solverAddress))
                    {
                        continue;
                    }

                    var sourceCurrency = currencies
                        .FirstOrDefault(x => x.Asset == starknetTokenCommittedEventResult.Value.SourceAsset
                            && x.Network.Name == networkName);

                    if (sourceCurrency is null)
                    {
                        continue;
                    }

                    var commitMessage = new HTLCCommitEventMessage
                    {
                        TxId = htlcEvent.TransactionHash,
                        Id = starknetTokenCommittedEventResult.Value.Id.ToHexBigInteger().HexValue.ToString(),
                        Amount = Web3.Convert.FromWei(BigInteger.Parse(starknetTokenCommittedEventResult.Value.AmountInBaseUnits), sourceCurrency.Decimals),
                        AmountInWei = starknetTokenCommittedEventResult.Value.AmountInBaseUnits,
                        ReceiverAddress = solverAddress,
                        SourceNetwork = networkName,
                        SenderAddress = FormatAddress(starknetTokenCommittedEventResult.Value.SenderAddress),
                        SourceAsset = starknetTokenCommittedEventResult.Value.SourceAsset,
                        DestinationAddress = starknetTokenCommittedEventResult.Value.DestinationAddress,
                        DestinationNetwork = starknetTokenCommittedEventResult.Value.DestinationNetwork,
                        DestinationAsset = starknetTokenCommittedEventResult.Value.DestinationAsset,
                        TimeLock = (long)starknetTokenCommittedEventResult.Value.Timelock
                    };

                    result.HTLCCommitEventMessages.Add(commitMessage);
                }
            }
            catch (Exception ex)
            {
                return Result.Fail($"Error processing event: {htlcEvent.TransactionHash} for block {htlcEvent.BlockNumber}: Reason {ex.Message}");
            }
        }

        return result;
    }

    public override async Task<Result<BlockNumberResponse>> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        var network = await dbContext.Networks
          .Include(x => x.Nodes)
          .SingleAsync(x => EF.Functions.ILike(x.Name, networkName));

        var node = network.Nodes.SingleOrDefault(x => x.Type == NodeType.DepositTracking);

        if (node is null)
        {
            return Result.Fail($"Node with type: {NodeType.Primary} is not configured in {networkName}");
        }

        var rpcClient = new StarknetRpcClient(new Uri(node.Url));

        var lastBlock = await rpcClient
                  .SendRequestAsync<int>(
                      new RpcRequest(
                        id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        method: StarknetConstants.RpcMethods.BlockNumber));

        var getBlockResponse = await rpcClient
                       .SendRequestAsync<GetBlockResponse>(
                           new RpcRequest(
                              id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                              method: StarknetConstants.RpcMethods.GetBlockWithHashes,
                              parameterList: new GetBlockRequest
                              {
                                  BlockNumber = lastBlock
                              }));

        return new BlockNumberResponse
        {
            BlockNumber = lastBlock.ToString(),
            BlockHash = getBlockResponse.BlockHash
        };
    }

    public override async Task<Result<string>> GetNextNonceAsync(string networkName, string address, string referenceId)
    {
        var network = await dbContext.Networks
             .Include(x => x.Nodes)
             .SingleOrDefaultAsync(x => x.Name == networkName);

        if (network == null)
        {
            return Result.Fail($"Network {networkName} is not configured");
        }

        var node = network.Nodes.SingleOrDefault(x => x.Type == NodeType.Primary);

        if (node is null)
        {
            return Result.Fail($"Node with type: {NodeType.Primary} is not configured in {networkName}");
        }
        var web3Client = new StarknetRpcClient(new Uri(node.Url));

        var formattedAddress = FormatAddress(address);

        await using var distributedLock = await distributedLockFactory.CreateLockAsync(
           resource: RedisHelper.BuildLockKey(networkName, formattedAddress),
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

            var currentNonceRedis = await cache.StringGetAsync(RedisHelper.BuildNonceKey(networkName, formattedAddress));

            if (currentNonceRedis != RedisValue.Null)
            {
                curentNonce = BigInteger.Parse(currentNonceRedis!);
            }

            var nonceHex = await web3Client
                    .SendRequestAsync<string>(
                        new RpcRequest(
                           id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                           method: StarknetConstants.RpcMethods.GetNonce,
                           new[] { "pending", formattedAddress }));

            var nonce = new HexBigInteger(nonceHex).Value;

            if (nonce <= curentNonce)
            {
                curentNonce++;
                nonce = curentNonce;
            }
            else
            {
                curentNonce = nonce;
            }

            await cache.StringSetAsync(RedisHelper.BuildNonceKey(networkName, formattedAddress),
               curentNonce.ToString(),
               expiry: TimeSpan.FromDays(7));

            return Result.Ok(curentNonce.ToString());
        }
        catch (Exception ex)
        {
            return Result.Fail($"Failed to retrieve nonce in network: {networkName}. Address: {formattedAddress}");
        }
    }

    public override Task<Result<string>> GetSpenderAllowanceAsync(string networkName, string ownerAddress, string spenderAddress, string asset)
    {
        throw new NotImplementedException();
    }

    public override Task<Result<bool>> ValidateAddLockSignatureAsync(string networkName, AddLockSigValidateRequest request)
    {
        throw new NotImplementedException();
    }

    public override bool ValidateAddress(string address)
    {
        bool result = false;

        var addressInt = new HexBigInteger(address);

        if (addressInt.InRange(Mask221, Mask251))
        {
            result = Regex.Match(address.AddAddressPadding(), "^(0x)?[0-9a-fA-F]{64}$").Success;
        }

        return result;
    }

    public Result<TransactionStatuses> ValidateTransactionStatus(string finalityStatus, string executionStatus)
    {
        if (StarknetConstants.TransferStatuses.Confirmed.Any(x => x == finalityStatus) && StarknetConstants.ExecutionStatuses.Confirmed.Any(x => x == executionStatus))
        {
            return Result.Ok(TransactionStatuses.Completed);
        }

        if (StarknetConstants.TransferStatuses.Confirmed.Any(x => x == finalityStatus) && StarknetConstants.ExecutionStatuses.Failed.Any(x => x == executionStatus))
        {
            return Result.Ok(TransactionStatuses.Failed);
        }

        if (StarknetConstants.TransferStatuses.Failed.Any(x => x == finalityStatus))
        {
            return Result.Ok(TransactionStatuses.Failed);
        }

        if (StarknetConstants.TransferStatuses.Pending.Any(x => x == finalityStatus))
        {
            return Result.Ok(TransactionStatuses.Pending);
        }

        return Result.Fail($"Cannot determine status {finalityStatus} or execution status {executionStatus}");
    }
}
