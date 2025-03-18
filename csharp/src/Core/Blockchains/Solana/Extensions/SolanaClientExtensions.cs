using System.Reflection;
using System.Text.Json;
using Flurl.Http;
using Solnet.Rpc;
using Solnet.Rpc.Core.Http;
using Solnet.Rpc.Messages;
using Solnet.Rpc.Types;
using Train.Solver.Core.Blockchains.Solana.Helpers;
using Train.Solver.Core.Blockchains.Solana.Models;

namespace Train.Solver.Core.Blockchains.Solana.Extensions;

public static class SolanaClientExtensions
{
    private static readonly Dictionary<string, MethodInfo> methodDict = [];

    private static readonly Dictionary<string, object> _sendTransactionParams = new()
    {
        { "skipPreflight", true },
        { "preflightCommitment", Commitment.Finalized },
        { "encoding", BinaryEncoding.Base64 },
        { "maxRetries", 200},
    };

    private const string _buildRequestMethodName = "BuildRequest";
    private const string _sendRequestMethodName = "SendRequest";

    public static async Task<SolanaParsedTransactionResponse> GetParsedTransactionAsync(
        this IRpcClient client,
        string transactionId)
    {
        var response = await client.NodeAddress
            .AllowAnyHttpStatus()
            .PostJsonAsync(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "getTransaction",
                @params = new object[]
                {
                    transactionId,
                    new {
                        encoding = "jsonParsed",
                        maxSupportedTransactionVersion = 0,
                    }
                }
            });

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            var errorMessage = await response.ResponseMessage.Content.ReadAsStringAsync();

            throw new Exception($"Get Transaction with id: {transactionId} fails due to message: {errorMessage}, status code: {response.ResponseMessage.StatusCode}");
        }

        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new ParsedInstructionDataConverter(),
                new InstructionDataConverter()
            }
        };

        var jsonString = await response.ResponseMessage.Content.ReadAsStringAsync();

        var parsedResult = JsonSerializer.Deserialize<SolanaParsedTransactionResponse>(jsonString, options);

        if (parsedResult is null)
        {
            throw new Exception(
                $"Failed to get parsed transaction. TxHash: {transactionId} Status code: {response.StatusCode}");
        }

        if (parsedResult.Result is null)
        {
            throw new Exception($"Rpc returned empty body. TxHash {transactionId}");
        }

        return parsedResult;
    }

    public static async Task<SolanaAccountInfoResponse> GetParsedAccountInfoAsync(
       this IRpcClient client,
       string address)
    {
        var response = await client.NodeAddress
            .AllowAnyHttpStatus()
            .PostJsonAsync(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "getAccountInfo",
                @params = new object[]
                {
                    address,
                    new { encoding = "jsonParsed" }
                }
            });

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            var errorMessage = await response.ResponseMessage.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get account info fails due to message: {errorMessage}, status code: {response.ResponseMessage.StatusCode}");
        }

        var result = await response.GetJsonAsync<SolanaAccountInfoResponse>();

        return result;
    }

    public static async Task<SolanaBlockEventResponse> GetParsedEventBlockAsync(
        this IRpcClient client,
        int block)
    {
            var response = await client.NodeAddress
                .AllowAnyHttpStatus()
                .PostJsonAsync(new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "getBlock",
                    @params = new object[]
                    {
                        block,
                        new
                        {
                            encoding = "jsonParsed",
                            maxSupportedTransactionVersion = 0,
                            rewards = false,
                            transactionDetails = "full"
                        }
                    }
                });

            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                var errorMessage = await response.ResponseMessage.Content.ReadAsStringAsync();

                throw new Exception($"Get parsed block fails due to message: {errorMessage}, status code: {response.ResponseMessage.StatusCode}");
            }

            var result = await response.GetJsonAsync<SolanaBlockEventResponse>();

            return result;
        
    }

    public static async Task<SolanaSignatureStatusResponse> GetSignatureStatusAsync(
        this IRpcClient client,
        string transactionId)
    {
        var response = await client.NodeAddress
            .AllowAnyHttpStatus()
            .PostJsonAsync(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "getSignatureStatuses",
                @params = new object[]
                    {
                        new[] { transactionId },
                        new
                        {
                            searchTransactionHistory = true
                        }
                    }
            });

        if (!response.ResponseMessage.IsSuccessStatusCode)
        {
            var errorMessage = await response.ResponseMessage.Content.ReadAsStringAsync();

            throw new Exception(
                $"Get transaction status fails due to message: {errorMessage}, status code: {response.ResponseMessage.StatusCode}");
        }

        var parsedResult = await response.GetJsonAsync<SolanaSignatureStatusResponse>();

        if (parsedResult.Result is null)
        {
            throw new Exception($"Rpc returned empty body for transaction status check. TxHash {transactionId}");
        }

        return parsedResult;
    }

    public static async Task<RequestResult<string>> SendSolanaTransactionAsync(
        this IRpcClient client,
        byte[] transaction)
    {
        var buildRequestMethod = GetBuildRequestMethod(client);
        var sendRequestMethod = GetSendRequestMethod(client);

        var request = (JsonRpcRequest)buildRequestMethod.Invoke(
            client,
            [
                "sendTransaction",
                new List<object>
                {
                    Convert.ToBase64String(transaction),
                    _sendTransactionParams
                }
            ])!;

        return await (Task<RequestResult<string>>)sendRequestMethod.Invoke(client, [request]);
    }

    private static MethodInfo GetBuildRequestMethod(IRpcClient client)
    {
        if (!methodDict.TryGetValue(_buildRequestMethodName, out var buildRequestMethod))
        {
            buildRequestMethod = client
                .GetType()
                .GetMethod(
                     name: _buildRequestMethodName,
                     bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
                     types:
                     [
                        typeof(string),
                         typeof(IList<object>)
                     ])!
                .MakeGenericMethod([typeof(string)]);

            methodDict.TryAdd(_buildRequestMethodName, buildRequestMethod);
        }

        return buildRequestMethod;
    }

    private static MethodInfo GetSendRequestMethod(IRpcClient client)
    {
        if (!methodDict.TryGetValue(_sendRequestMethodName, out var buildRequestMethod))
        {
            buildRequestMethod = client
                .GetType()
                .GetMethod(
                     name: _sendRequestMethodName,
                     bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
                     types:
                     [
                        typeof(JsonRpcRequest),
                     ])!
                .MakeGenericMethod([typeof(string)]);

            methodDict.TryAdd(_sendRequestMethodName, buildRequestMethod);
        }

        return buildRequestMethod;
    }
}
