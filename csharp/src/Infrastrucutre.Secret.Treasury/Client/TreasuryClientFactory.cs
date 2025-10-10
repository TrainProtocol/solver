using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Train.Solver.Infrastrucutre.Secret.Treasury.Client;
public static class TreasuryClientFactory
{
    public static ITreasuryClient Create(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be null or empty.", nameof(baseUrl));

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions),
            Buffered = true
        };

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMinutes(10)
        };

        return RestService.For<ITreasuryClient>(httpClient, refitSettings);
    }
}