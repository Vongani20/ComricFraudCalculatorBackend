using System.Net;
using System.Net.Http.Json;
using ComricFraudCalculatorBackend.Enums;
using ComricFraudCalculatorBackend.Models.Requests;
using ComricFraudCalculatorBackend.Models.Responses;

namespace ComricFraudCalculatorBackend.IntegrationTests;

[Collection("Integration")]
public class FraudSignalsEndpointTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task FraudSignals_ReturnNoTenantAttribution()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();

        var postResponse = await client.PostAsJsonAsync("/api/v1/hr-events", new SubmitHrEventRequest
        {
            IdNumber = "7701015800086",
            EventType = HrEventType.GhostEmployee,
            EventDate = DateTime.UtcNow,
            EmployerName = "Secret Corp",
            VerificationStatus = VerificationStatus.Denied
        });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var response = await client.GetAsync("/api/v1/fraud-signals");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("tenantId", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret Corp", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdCheck_HashesInputBeforeLookup()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();

        var postResponse = await client.PostAsJsonAsync("/api/v1/mno-events", new SubmitMnoEventRequest
        {
            IdNumber = "8801015800087",
            Msisdn = "0831234567",
            EventType = MnoEventType.SIMSwap,
            EventDate = DateTime.UtcNow,
            ApplicationChannel = ApplicationChannel.Online,
            OutletOrDealer = "Online Portal",
            FlagReason = "Known fraud pattern"
        });
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var checkResponse = await client.PostAsJsonAsync("/api/v1/lookup/id-check", new IdCheckRequest
        {
            IdNumber = "8801015800087"
        });

        Assert.Equal(HttpStatusCode.OK, checkResponse.StatusCode);
        var result = await checkResponse.Content.ReadFromJsonAsync<IdCheckResponse>(TestHttpClientExtensions.JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.MatchFound);
        Assert.DoesNotContain("8801015800087", result.IdNumberHash);
    }

    [Fact]
    public async Task DashboardStats_ReturnsAggregates()
    {
        var client = fixture.Factory.CreateAuthenticatedClient("Dashboard.Read Events.Read");
        var response = await client.GetAsync("/api/v1/dashboard/stats");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var stats = await response.Content.ReadFromJsonAsync<DashboardStatsResponse>(TestHttpClientExtensions.JsonOptions);
        Assert.NotNull(stats);
        Assert.True(stats.TotalHrEvents >= 0);
    }
}
