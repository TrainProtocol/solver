using System.Net.Http.Headers;
using Nethereum.JsonRpc.Client;
using Nethereum.JsonRpc.Client.RpcMessages;
using Newtonsoft.Json;

namespace Train.Solver.Blockchains.Starknet.Helpers;

public class StarknetRpcClient : RpcClient
{
    private readonly JsonSerializerSettings? _jsonSerializerSettings;
    private readonly HttpClient _httpClient;

    public StarknetRpcClient(
        Uri baseUrl,
        AuthenticationHeaderValue? authHeaderValue = null,
        JsonSerializerSettings? jsonSerializerSettings = null,
        HttpClientHandler? httpClientHandler = null,
        ILogger? log = null)
        : base(baseUrl, authHeaderValue, jsonSerializerSettings, httpClientHandler, log)
    {
        _jsonSerializerSettings = jsonSerializerSettings;
        _httpClient = new HttpClient { BaseAddress = baseUrl };
    }

    protected override async Task<RpcResponseMessage> SendAsync(RpcRequestMessage request, string? route = null)
    {
        try
        {
            var rpcRequestJson = JsonConvert.SerializeObject(request, _jsonSerializerSettings);
            var httpContent = new StringContent(rpcRequestJson, new MediaTypeHeaderValue("application/json"));
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(ConnectionTimeout);

            var httpResponseMessage = await _httpClient.PostAsync(route, httpContent, cancellationTokenSource.Token).ConfigureAwait(false);
            httpResponseMessage.EnsureSuccessStatusCode();

            var stream = await httpResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using (var streamReader = new StreamReader(stream))
            using (var reader = new JsonTextReader(streamReader))
            {
                var serializer = JsonSerializer.Create(_jsonSerializerSettings);
                var message = serializer.Deserialize<RpcResponseMessage>(reader);
                return message;
            }
        }
        catch (TaskCanceledException ex)
        {
            var exception = new RpcClientTimeoutException($"Rpc timeout after {ConnectionTimeout.TotalMilliseconds} milliseconds", ex);
            throw exception;
        }
        catch (Exception ex)
        {
            var exception = new RpcClientUnknownException("Error occurred when trying to send rpc requests(s): " + request.Method, ex);
            throw exception;
        }
    }
}
