using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ComricFraudCalculatorBackend.IntegrationTests;

public static class TestHttpClientExtensions
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static HttpClient CreateAuthenticatedClient(this CustomWebApplicationFactory factory, string scopes = "Events.Read Events.Write Signals.Read Audit.Read Dashboard.Read")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
        client.DefaultRequestHeaders.Add("X-Test-Scopes", scopes);
        return client;
    }
}
