using System.Net;
using System.Net.Http.Json;
using ComricFraudCalculatorBackend.Enums;
using ComricFraudCalculatorBackend.Models.Requests;
using ComricFraudCalculatorBackend.Models.Responses;

namespace ComricFraudCalculatorBackend.IntegrationTests;

public class IntegrationTestFixture : IAsyncLifetime
{
    public CustomWebApplicationFactory Factory { get; } = new();

    public async Task InitializeAsync() => await Factory.InitializeDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}

[Collection("Integration")]
public class HrEventsEndpointTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task GetHrEvents_WithoutAuth_ReturnsUnauthorized()
    {
        var client = fixture.Factory.CreateClient();
        var response = await client.GetAsync("/api/v1/hr-events");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHrEvents_WithReadScope_ReturnsOk()
    {
        var client = fixture.Factory.CreateAuthenticatedClient("Events.Read");
        var response = await client.GetAsync("/api/v1/hr-events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostHrEvent_WithWriteScope_CreatesEventAndSignal()
    {
        var client = fixture.Factory.CreateAuthenticatedClient("Events.Read Events.Write Signals.Read");

        var request = new SubmitHrEventRequest
        {
            IdNumber = "8501015800084",
            EventType = HrEventType.GhostEmployee,
            EventDate = DateTime.UtcNow,
            EmployerName = "Test Employer",
            EmployeeNumber = "EMP-001",
            VerificationStatus = VerificationStatus.Denied,
            Notes = "Integration test event"
        };

        var postResponse = await client.PostAsJsonAsync("/api/v1/hr-events", request);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var created = await postResponse.Content.ReadFromJsonAsync<HrEventResponse>(TestHttpClientExtensions.JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.RiskScore > 0);

        var signalsResponse = await client.GetAsync("/api/v1/fraud-signals?activeOnly=true");
        var signals = await signalsResponse.Content.ReadFromJsonAsync<FraudSignalListResponse>(TestHttpClientExtensions.JsonOptions);
        Assert.NotNull(signals);
        Assert.Contains(signals.Signals, s => s.SignalType == SignalType.HR_Alert);
    }

    [Fact]
    public async Task PostHrEvent_WithoutWriteScope_ReturnsForbidden()
    {
        var client = fixture.Factory.CreateAuthenticatedClient("Events.Read");
        var request = new SubmitHrEventRequest
        {
            IdNumber = "9001015800085",
            EventType = HrEventType.IdentityFraud,
            EventDate = DateTime.UtcNow,
            EmployerName = "Test",
            VerificationStatus = VerificationStatus.Denied
        };

        var response = await client.PostAsJsonAsync("/api/v1/hr-events", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
