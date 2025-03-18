using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Newtonsoft.Json;
using RedLockNet;
using StackExchange.Redis;
using Temporalio.Activities;
using Train.Solver.Core.Activities;
using Train.Solver.Core.Blockchains.Starknet.Extensions;
using Train.Solver.Core.Blockchains.Starknet.Helpers;
using Train.Solver.Core.Blockchains.Starknet.Models;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Helpers;
using Train.Solver.Core.Models;
using Train.Solver.Data;
using Train.Solver.Data.Entities;

namespace Train.Solver.Core.Blockchains.Starknet.Activities;

public class StarknetBlockchainActivities(
    SolverDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    IDatabase cache,
    IDistributedLockFactory distributedLockFactory) : BlockchainActivitiesBase(dbContext)
{
    public static NetworkGroup NetworkGroup => NetworkGroup.Starknet;
    private static readonly BigInteger BigIntTwo = new BigInteger(2);
    private static readonly BigInteger Mask221 = BigInteger.Pow(BigIntTwo, 221);
    private static readonly BigInteger Mask251 = BigInteger.Pow(BigIntTwo, 251);

    public override Task<PrepareTransactionResponse> BuildTransactionAsync(string networkName,
        TransactionType transactionType, string args)
    {
        throw new TaskQueueMismatchException();
    }

    [Activity(name: $"{nameof(NetworkGroup.Starknet)}{nameof(GetNonceAsync)}")]
    public override Task<string> GetNonceAsync(string networkName, string address, string referenceId)
    {
        return base.GetNonceAsync(networkName, address, referenceId);
    }

    [Activity(name: $"{nameof(NetworkGroup.Starknet)}{nameof(EnsureSufficientBalanceAsync)}")]
    public override Task EnsureSufficientBalanceAsync(string networkName, string address, string asset, decimal amount)
    {
        return base.EnsureSufficientBalanceAsync(networkName, address, asset, amount);
    }

    [Activity(name: $"{nameof(NetworkGroup.Starknet)}{nameof(GetSpenderAddressAsync)}")]
    public override Task<string> GetSpenderAddressAsync(string networkName, string asset)
    {
        return base.GetSpenderAddressAsync(networkName, asset);
    }

    public override Task<Fee> EstimateFeeAsync(string network, EstimateFeeRequest request)
    {
        throw new TaskQueueMismatchException();
    }

    [Activity(name: $"{nameof(NetworkGroup.Starknet)}{nameof(FormatAddress)}")]
    public override string FormatAddress(string address) => address.AddAddressPadding().ToLowerInvariant();

    public override Task<string> GenerateAddressAsync(string networkName)
    {
        throw new TaskQueueMismatchException();
    }

    [Activity(name: $"{nameof(NetworkGroup.Starknet)}{nameof(GetBalanceAsync)}")]
    public override async Task<BalanceModel> GetBalanceAsync(string networkName, string address, string asset)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());

        var token = network.Tokens.Single(x => x.Asset.ToUpper() == asset.ToUpper());

        if (token.TokenContract is null)
        {
            throw new ArgumentNullException(nameof(token.TokenContract),
                $"Token contract is not configured for {token.Asset} in {networkName} network");
        }

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new ArgumentException($"Nodes are not configured for {network.Name} network", nameof(nodes));
        }

        var requestJson = BuildRequestPayload(address, token);

        var result = await ResilientNodeHelper
            .GetDataFromNodesAsync(nodes,
                async url => await GetBalance(url, requestJson));

        return new BalanceModel
        {
            AmountInWei = result.ToString(),
            Amount = Web3.Convert.FromWei(result, token.Decimals),
            Decimals = token.Decimals,
        };
    }

  
    [Activity(name: $"{nameof(NetworkGroup.Starknet)}{nameof(GetTransactionAsync)}")]
    public override async Task<TransactionModel> GetTransactionAsync(string networkName,
        string transactionId)
    {
        var network = await dbContext.Networks
            .Include(x => x.Tokens)
            .Include(x => x.Nodes)
            .Include(x => x.ManagedAccounts)
            .Include(x => x.DeployedContracts)
            .SingleAsync(x => x.Name.ToUpper() == networkName.ToUpper());


        var node = network.Nodes.Single(x => x.Type == NodeType.DepositTracking);

        var rpcClient = new StarknetRpcClient(new Uri(node.Url));

        var managedAccountAddress =
            FormatAddress(network.ManagedAccounts.Single(x => x.Type == AccountType.LP).Address);

        TransactionModel? transactionModel = null;

        var transactionStatusResponse = await rpcClient.SendRequestAsync<StatusResponse>(new RpcRequest(
            id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            method: StarknetConstants.RpcMethods.GetTransactionStatus,
            transactionId!));

        var statusResult = ValidateTransactionStatus(transactionStatusResponse.FinalityStatus,
            transactionStatusResponse.ExecutionStatus);

        if (statusResult == TransactionStatus.Failed)
        {
            throw new TransactionFailedException($"Transaction receipt in {networkName} indicates failure");
        }

        var transactionReceiptResponse = await rpcClient
            .SendRequestAsync<TransactionResponse>(
                new RpcRequest(
                    id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    method: StarknetConstants.RpcMethods.GetReceipt,
                    transactionId!));

        var feeInWei = new HexBigInteger(transactionReceiptResponse.ActualFee.Amount).Value;
        var feeDecimals = 18;

        transactionModel = new TransactionModel
        {
            TransactionHash = FormatAddress(transactionReceiptResponse.TransactionHash),
            Confirmations = statusResult == TransactionStatus.Initiated ? 0 : 1,
            Status = statusResult,
            FeeAsset = "ETH",
            FeeAmount = Web3.Convert.FromWei(feeInWei, feeDecimals),
            Timestamp = DateTimeOffset.UtcNow,
            NetworkName = networkName,
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

            transactionModel.Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(getBlockResponse.Timestamp * 1000);
        }

        return transactionModel;
    }

    [Activity(name: $"{nameof(NetworkGroup.Starknet)}{nameof(GetEventsAsync)}")]
    public override async Task<HTLCBlockEvent> GetEventsAsync(string networkName, ulong fromBlock, ulong toBlock)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .Include(x => x.Tokens)
            .Include(x => x.ManagedAccounts)
            .Include(x => x.DeployedContracts)
            .SingleAsync(x =>
                x.Name.ToUpper() == networkName.ToUpper());

        var node = network!.Nodes.First(x => x.Type == NodeType.DepositTracking);

        var htlcTokenContract = network.DeployedContracts
            .First(x => x.Type == ContarctType.HTLCTokenContractAddress);

        var solverAccount = network.ManagedAccounts
            .First(x => x.Type == AccountType.LP);

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

    [Activity(name: $"{nameof(NetworkGroup.Starknet)}{nameof(GetLastConfirmedBlockNumberAsync)}")]
    public override async Task<BlockNumberModel> GetLastConfirmedBlockNumberAsync(string networkName)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .SingleAsync(x => EF.Functions.ILike(x.Name, networkName));

        var node = network.Nodes.SingleOrDefault(x => x.Type == NodeType.DepositTracking);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node),
                $"Node with type: {NodeType.DepositTracking} is not configured in {networkName}");
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

        return new ()
        {
            BlockNumber = (ulong)lastBlock,
            BlockHash = getBlockResponse.BlockHash
        };
    }

    protected override async Task<string> GetNextNonceAsync(string networkName, string address, string referenceId)
    {
        var network = await dbContext.Networks
            .Include(x => x.Nodes)
            .SingleAsync(x => x.Name == networkName);
        
        var node = network.Nodes.Single(x => x.Type == NodeType.Primary);

        var web3Client = new StarknetRpcClient(new Uri(node.Url));

        var formattedAddress = FormatAddress(address);

        await using var distributedLock = await distributedLockFactory.CreateLockAsync(
            resource: RedisHelper.BuildLockKey(networkName, formattedAddress),
            retryTime: TimeSpan.FromSeconds(1),
            waitTime: TimeSpan.FromSeconds(20),
            expiryTime: TimeSpan.FromSeconds(25));

        if (!distributedLock.IsAcquired)
        {
            throw new SynchronizationLockException("Failed to acquire the lock");
        }

        var currentNonce = new BigInteger(-1);

        var currentNonceRedis = await cache.StringGetAsync(RedisHelper.BuildNonceKey(networkName, formattedAddress));

        if (currentNonceRedis != RedisValue.Null)
        {
            currentNonce = BigInteger.Parse(currentNonceRedis);
        }

        var nonceHex = await web3Client
            .SendRequestAsync<string>(
                new RpcRequest(
                    id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    method: StarknetConstants.RpcMethods.GetNonce,
                    new[] { "pending", formattedAddress }));

        var nonce = new HexBigInteger(nonceHex).Value;

        if (nonce <= currentNonce)
        {
            currentNonce++;
            nonce = currentNonce;
        }
        else
        {
            currentNonce = nonce;
        }

        await cache.StringSetAsync(RedisHelper.BuildNonceKey(networkName, formattedAddress),
            currentNonce.ToString(),
            expiry: TimeSpan.FromDays(7));

        return currentNonce.ToString();
    }

    public override Task<bool> ValidateAddLockSignatureAsync(string networkName, AddLockSignatureRequest request)
    {
        throw new TaskQueueMismatchException();
    }

    [Activity(name: $"{nameof(NetworkGroup.Starknet)}{nameof(ValidateAddress)}")]
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


    private async Task<HTLCBlockEvent> TrackBlockEventsAsync(
        string networkName,
        IClient web3Client,
        List<Token> tokens,
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
            if (htlcEvent.Keys.Contains(StarknetConstants.EventIds.HTLCLockEventId))
            {
                var deserializedTokenLockedEventResult = htlcEvent.DeserializeLockEventData();

                var lockMessage = new HTLCLockEventMessage
                {
                    TxId = htlcEvent.TransactionHash,
                    HashLock = deserializedTokenLockedEventResult.Hashlock.ToHexBigInteger().HexValue.AddAddressPadding(),
                    Id = deserializedTokenLockedEventResult.Id.ToHexBigInteger().HexValue.AddAddressPadding(),
                    TimeLock = (long)deserializedTokenLockedEventResult.Timelock,
                };

                result.HTLCLockEventMessages.Add(lockMessage);
            }
            else if (htlcEvent.Keys.Contains(StarknetConstants.EventIds.HTLCCommitEventId))
            {
                var starknetTokenCommittedEventResult = htlcEvent.DeserializeCommitEventData();

                if (FormatAddress(starknetTokenCommittedEventResult.SourceReciever)
                    != FormatAddress(solverAddress))
                {
                    continue;
                }

                var sourceToken = tokens
                    .FirstOrDefault(x => x.Asset == starknetTokenCommittedEventResult.SourceAsset
                        && x.Network.Name == networkName);

                if (sourceToken is null)
                {
                    continue;
                }

                var destinationToken = tokens
                    .FirstOrDefault(x => x.Asset == starknetTokenCommittedEventResult.DestinationAsset
                        && x.Network.Name == starknetTokenCommittedEventResult.DestinationNetwork);

                if (destinationToken is null)
                {
                    continue;
                }

                var commitMessage = new HTLCCommitEventMessage
                {
                    TxId = htlcEvent.TransactionHash,
                    Id = starknetTokenCommittedEventResult.Id.ToHexBigInteger().HexValue.AddAddressPadding(),
                    Amount = Web3.Convert.FromWei(
                            BigInteger.Parse(starknetTokenCommittedEventResult.AmountInBaseUnits),
                            sourceToken.Decimals),
                    AmountInWei = starknetTokenCommittedEventResult.AmountInBaseUnits,
                    ReceiverAddress = solverAddress,
                    SourceNetwork = networkName,
                    SenderAddress = FormatAddress(starknetTokenCommittedEventResult.SenderAddress),
                    SourceAsset = starknetTokenCommittedEventResult.SourceAsset,
                    DestinationAddress = starknetTokenCommittedEventResult.DestinationAddress,
                    DestinationNetwork = starknetTokenCommittedEventResult.DestinationNetwork,
                    DestinationAsset = starknetTokenCommittedEventResult.DestinationAsset,
                    TimeLock = (long)starknetTokenCommittedEventResult.Timelock,
                    DestinationNetwrokGroup = destinationToken.Network.Group,
                    SourceNetwrokGroup = sourceToken.Network.Group,
                };

                result.HTLCCommitEventMessages.Add(commitMessage);
            }

        }

        return result;
    }

    private static TransactionStatus ValidateTransactionStatus(string finalityStatus, string executionStatus)
    {
        if (StarknetConstants.TransferStatuses.Confirmed.Any(x => x == finalityStatus) &&
            StarknetConstants.ExecutionStatuses.Confirmed.Any(x => x == executionStatus))
        {
            return TransactionStatus.Completed;
        }

        if (StarknetConstants.TransferStatuses.Confirmed.Any(x => x == finalityStatus) &&
            StarknetConstants.ExecutionStatuses.Failed.Any(x => x == executionStatus))
        {
            return TransactionStatus.Failed;
        }

        if (StarknetConstants.TransferStatuses.Failed.Any(x => x == finalityStatus))
        {
            return TransactionStatus.Failed;
        }

        if (StarknetConstants.TransferStatuses.Pending.Any(x => x == finalityStatus))
        {
            return TransactionStatus.Initiated;
        }

        throw new ArgumentOutOfRangeException(nameof(TransactionStatus),
            $"Transaction status is not supported. Finality status: {finalityStatus}, Execution status: {executionStatus}");
    }
}