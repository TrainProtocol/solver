using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Nethereum.Hex.HexTypes;
using Newtonsoft.Json;
using RedLockNet;
using StackExchange.Redis;
using Temporalio.Activities;
using Train.Solver.Core.Entities;
using Train.Solver.Core.Exceptions;
using Train.Solver.Core.Extensions;
using Train.Solver.Core.Models;
using Nethereum.JsonRpc.Client;
using Nethereum.Web3;
using Train.Solver.Blockchains.Starknet.Models;
using Train.Solver.Blockchains.Starknet.Extensions;
using Train.Solver.Blockchains.Starknet.Helpers;
using Train.Solver.Core.Workflows.Helpers;
using Train.Solver.Core.Workflows.Activities;
using Train.Solver.Core.Repositories;

namespace Train.Solver.Blockchains.Starknet.Activities;

public class StarknetBlockchainActivities(
    ISwapRepository swapRepository,
    INetworkRepository networkRepository,
    IHttpClientFactory httpClientFactory,
    IDatabase cache,
    IDistributedLockFactory distributedLockFactory) : BlockchainActivitiesBase(networkRepository, swapRepository), IStarknetBlockchainActivities
{
    private static readonly BigInteger BigIntTwo = new BigInteger(2);
    private static readonly BigInteger Mask221 = BigInteger.Pow(BigIntTwo, 221);
    private static readonly BigInteger Mask251 = BigInteger.Pow(BigIntTwo, 251);

    public override Task<PrepareTransactionResponse> BuildTransactionAsync(TransactionBuilderRequest request)
    {
        throw new TaskQueueMismatchException();
    }

    public override Task EnsureSufficientBalanceAsync(SufficientBalanceRequest request)
    {
        throw new TaskQueueMismatchException();
    }

    [Activity(name: $"{nameof(NetworkType.Starknet)}{nameof(GetSpenderAddressAsync)}")]
    public override Task<string> GetSpenderAddressAsync(SpenderAddressRequest request)
    {
        return base.GetSpenderAddressAsync(request);
    }

    public override Task<Fee> EstimateFeeAsync(EstimateFeeRequest request)
    {
        throw new TaskQueueMismatchException();
    }

    protected override string FormatAddress(AddressRequest request) => request.Address.AddAddressPadding().ToLowerInvariant();

    [Activity(name: $"{nameof(NetworkType.Starknet)}{nameof(GetBalanceAsync)}")]
    public override async Task<BalanceResponse> GetBalanceAsync(BalanceRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var token = network.Tokens.Single(x => x.Asset.ToUpper() == request.Asset.ToUpper());

        if (token.TokenContract is null)
        {
            throw new ArgumentNullException(nameof(token.TokenContract),
                $"Token contract is not configured for {token.Asset} in {request.NetworkName} network");
        }

        var nodes = network.Nodes;

        if (!nodes.Any())
        {
            throw new ArgumentException($"Nodes are not configured for {network.Name} network", nameof(nodes));
        }

        var requestJson = BuildRequestPayload(token, request.Address);

        var result = await ResilientNodeHelper
            .GetDataFromNodesAsync(nodes,
                async url => await GetBalance(url, requestJson));

        return new BalanceResponse
        {
            AmountInWei = result.ToString(),
            Amount = Web3.Convert.FromWei(result, token.Decimals),
            Decimals = token.Decimals,
        };
    }

    private async Task<Core.Models.TransactionResponse> GetTransactionAsync(
        Network network,
        string transactionId)
    {
        var node = network.Nodes.Single(x => x.Type == NodeType.Primary);

        var rpcClient = new StarknetRpcClient(new Uri(node.Url));

        Core.Models.TransactionResponse? transactionModel = null;

        var transactionStatusResponse = await rpcClient.SendRequestAsync<StatusResponse>(new RpcRequest(
            id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            method: StarknetConstants.RpcMethods.GetTransactionStatus,
            transactionId!));

        var statusResult = ValidateTransactionStatus(transactionStatusResponse.FinalityStatus,
            transactionStatusResponse.ExecutionStatus);

        if (statusResult == TransactionStatus.Failed)
        {
            throw new TransactionFailedException($"Transaction receipt in {network.Name} indicates failure");
        }

        var transactionReceiptResponse = await rpcClient
            .SendRequestAsync<Models.TransactionResponse>(
                new RpcRequest(
                    id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    method: StarknetConstants.RpcMethods.GetReceipt,
                    transactionId!));

        if (transactionReceiptResponse is null)
        {
            return null;
        }

        var feeInWei = new HexBigInteger(transactionReceiptResponse.ActualFee.Amount).Value;
        var feeDecimals = 18;

        transactionModel = new Core.Models.TransactionResponse
        {
            TransactionHash = FormatAddress(new() { Address = transactionReceiptResponse.TransactionHash }),
            Confirmations = statusResult == TransactionStatus.Initiated ? 0 : 1,
            Status = statusResult,
            FeeAsset = "ETH",
            FeeAmount = Web3.Convert.FromWei(feeInWei, feeDecimals),
            Timestamp = DateTimeOffset.UtcNow,
            NetworkName = network.Name,
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

    [Activity(name: $"{nameof(NetworkType.Starknet)}{nameof(GetEventsAsync)}")]
    public override async Task<HTLCBlockEventResponse> GetEventsAsync(EventRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var node = network!.Nodes.First(x => x.Type == NodeType.DepositTracking);
        var htlcTokenContract = network.Contracts
            .First(x => x.Type == ContarctType.HTLCTokenContractAddress);

        var solverAccount = network.ManagedAccounts
            .First(x => x.Type == AccountType.LP);

        var currencies = await networkRepository.GetTokensAsync();
        var rpcClient = new StarknetRpcClient(new Uri(node.Url));

        return await TrackBlockEventsAsync(
            network.Name,
            rpcClient,
            currencies,
            solverAccount.Address,
            fromBlock: (int)request.FromBlock,
            toBlock: (int)request.ToBlock,
            htlcTokenContract.Address);
    }

    [Activity(name: $"{nameof(NetworkType.Starknet)}{nameof(GetLastConfirmedBlockNumberAsync)}")]
    public override async Task<BlockNumberResponse> GetLastConfirmedBlockNumberAsync(BaseRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var node = network.Nodes.SingleOrDefault(x => x.Type == NodeType.DepositTracking);

        if (node is null)
        {
            throw new ArgumentNullException(nameof(node),
                $"Node with type: {NodeType.DepositTracking} is not configured in {request.NetworkName}");
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

        return new()
        {
            BlockNumber = (ulong)lastBlock,
            BlockHash = getBlockResponse.BlockHash
        };
    }
    public override Task<bool> ValidateAddLockSignatureAsync(AddLockSignatureRequest request)
    {
        throw new TaskQueueMismatchException();
    }

    protected override bool ValidateAddress(AddressRequest request)
    {
        bool result = false;

        var addressInt = new HexBigInteger(request.Address);

        if (addressInt.InRange(Mask221, Mask251))
        {
            result = Regex.Match(request.Address.AddAddressPadding(), "^(0x)?[0-9a-fA-F]{64}$").Success;
        }

        return result;
    }

    private static string BuildRequestPayload(Token token, string address)
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

    private async Task<HTLCBlockEventResponse> TrackBlockEventsAsync(
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

        var result = new HTLCBlockEventResponse();

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

                if (FormatAddress(new() { Address = starknetTokenCommittedEventResult.SourceReciever })
                    != FormatAddress(new() { Address = solverAddress }))
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
                    SenderAddress = FormatAddress(new() { Address = starknetTokenCommittedEventResult.SenderAddress }),
                    SourceAsset = starknetTokenCommittedEventResult.SourceAsset,
                    DestinationAddress = starknetTokenCommittedEventResult.DestinationAddress,
                    DestinationNetwork = starknetTokenCommittedEventResult.DestinationNetwork,
                    DestinationAsset = starknetTokenCommittedEventResult.DestinationAsset,
                    TimeLock = (long)starknetTokenCommittedEventResult.Timelock,
                    DestinationNetworkType = destinationToken.Network.Type,
                    SourceNetworkType = sourceToken.Network.Type,
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

    [Activity(name: $"{nameof(NetworkType.Starknet)}{nameof(GetReservedNonceAsync)}")]
    public override Task<string> GetReservedNonceAsync(ReservedNonceRequest request)
    {
        return base.GetReservedNonceAsync(request);
    }

    public Task<string> SimulateTransactionAsync(StarknetPublishTransactionRequest request)
    {
        throw new TaskQueueMismatchException();
    }

    public Task<decimal> GetSpenderAllowanceAsync(AllowanceRequest request)
    {
        throw new TaskQueueMismatchException();
    }

    public Task<string> PublishTransactionAsync(StarknetPublishTransactionRequest request)
    {
        throw new TaskQueueMismatchException();
    }

    [Activity(name: $"{nameof(NetworkType.Starknet)}{nameof(GetBatchTransactionAsync)}")]
    public async Task<Core.Models.TransactionResponse> GetBatchTransactionAsync(GetBatchTransactionRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        Core.Models.TransactionResponse? transaction = null;

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

    public override async Task<Core.Models.TransactionResponse> GetTransactionAsync(GetTransactionRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var transaction = await GetTransactionAsync(network, request.TransactionId);

        if (transaction == null)
        {
            throw new TransactionNotComfirmedException("Transaction not confirmed");
        }

        return transaction;
    }

    [Activity(name: $"{nameof(NetworkType.Starknet)}{nameof(GetNextNonceAsync)}")]
    public override async Task<string> GetNextNonceAsync(NextNonceRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var node = network.Nodes.Single(x => x.Type == NodeType.Primary);
        var web3Client = new StarknetRpcClient(new Uri(node.Url));

        var formattedAddress = FormatAddress(new() { Address = request.Address });

        return await GetNextNonceAsync(web3Client, formattedAddress);
    }

    protected override async Task<string> GetCachedNonceAsync(NextNonceRequest request)
    {
        var network = await networkRepository.GetAsync(request.NetworkName);

        if (network is null)
        {
            throw new ArgumentNullException(nameof(network), $"Network {request.NetworkName} not found");
        }

        var node = network.Nodes.Single(x => x.Type == NodeType.Primary);
        var web3Client = new StarknetRpcClient(new Uri(node.Url));

        var formattedAddress = FormatAddress(new() { Address = request.Address });

        await using var distributedLock = await distributedLockFactory.CreateLockAsync(
            resource: RedisHelper.BuildLockKey(request.NetworkName, formattedAddress),
            retryTime: TimeSpan.FromSeconds(1),
            waitTime: TimeSpan.FromSeconds(20),
            expiryTime: TimeSpan.FromSeconds(25));

        if (!distributedLock.IsAcquired)
        {
            throw new SynchronizationLockException("Failed to acquire the lock");
        }

        var currentNonce = new BigInteger(-1);

        var currentNonceRedis = await cache.StringGetAsync(RedisHelper.BuildNonceKey(request.NetworkName, formattedAddress));

        if (currentNonceRedis != RedisValue.Null)
        {
            currentNonce = BigInteger.Parse(currentNonceRedis);
        }

        var nonce = BigInteger.Parse(await GetNextNonceAsync(web3Client, formattedAddress));

        if (nonce <= currentNonce)
        {
            currentNonce++;
            nonce = currentNonce;
        }
        else
        {
            currentNonce = nonce;
        }

        await cache.StringSetAsync(RedisHelper.BuildNonceKey(request.NetworkName, formattedAddress),
            currentNonce.ToString(),
            expiry: TimeSpan.FromDays(7));

        return currentNonce.ToString();
    }

    private async Task<string> GetNextNonceAsync(StarknetRpcClient client, string address)
    {
        var nonceHex = await client
            .SendRequestAsync<string>(
                new RpcRequest(
                    id: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    method: StarknetConstants.RpcMethods.GetNonce,
                    new[] { "pending", address }));

        return new HexBigInteger(nonceHex).Value.ToString();
    }
}
