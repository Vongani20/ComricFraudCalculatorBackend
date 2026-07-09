using System.Net;
using System.Net.Http.Json;
using ComricFraudCalculatorBackend.Enums;
using ComricFraudCalculatorBackend.Models.Requests;
using ComricFraudCalculatorBackend.Models.Responses;

namespace ComricFraudCalculatorBackend.IntegrationTests;

[Collection("Integration")]
public class MnoEventsEndpointTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task PostMnoEvent_CreatesEventWithRiskScore()
    {
        var client = fixture.Factory.CreateAuthenticatedClient();

        var request = new SubmitMnoEventRequest
        {
            IdNumber = "8501015800084",
            Msisdn = "0821234567",
            EventType = MnoEventType.NewSIMApplication,
            EventDate = DateTime.UtcNow,
            ApplicationChannel = ApplicationChannel.InStore,
            OutletOrDealer = "Test Outlet",
            FlagReason = "Velocity threshold exceeded"
        };

        var response = await client.PostAsJsonAsync("/api/v1/mno-events", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<MnoEventResponse>(TestHttpClientExtensions.JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.RiskScore >= 50);
    }

    [Fact]
    public async Task GetMnoEvents_ReturnsTenantScopedList()
    {
        var client = fixture.Factory.CreateAuthenticatedClient("Events.Read");
        var response = await client.GetAsync("/api/v1/mno-events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var events = await response.Content.ReadFromJsonAsync<List<MnoEventResponse>>(TestHttpClientExtensions.JsonOptions);
        Assert.NotNull(events);
    }
}
