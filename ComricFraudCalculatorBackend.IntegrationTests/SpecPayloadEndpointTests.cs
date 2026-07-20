using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ComricFraudCalculatorBackend.Enums;
using ComricFraudCalculatorBackend.Models.Responses;

namespace ComricFraudCalculatorBackend.IntegrationTests;

/// <summary>
/// Exercises PoC spec §4.2.1–4.2.3 payloads end-to-end against the test host.
/// </summary>
[Collection("Integration")]
public class SpecPayloadEndpointTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task Spec_421_422_423_SubmitHr_ThenMno_ThenListFraudSignals()
    {
        var client = fixture.Factory.CreateAuthenticatedClient(
            "Events.Read Events.Write Signals.Read");

        // 4.2.1 Submit HR Event
        var hrJson = """
            {
              "idNumber": "8501015800084",
              "eventType": "GhostEmployee",
              "eventDate": "2026-03-15T00:00:00Z",
              "employerName": "Acme Holdings (Pty) Ltd",
              "employeeNumber": "EMP-4521",
              "verificationStatus": "Denied",
              "notes": "Employee does not appear on company payroll. ID used in three separate applications."
            }
            """;

        var hrResponse = await client.PostAsync(
            "/api/v1/hr-events",
            new StringContent(hrJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, hrResponse.StatusCode);
        var hrBody = await hrResponse.Content.ReadAsStringAsync();
        Assert.Contains("8501015800084", hrBody);
        Assert.Contains("GhostEmployee", hrBody);

        // 4.2.2 Submit MNO Event
        var mnoJson = """
            {
              "idNumber": "8501015800084",
              "msisdn": "0821234567",
              "eventType": "NewSIMApplication",
              "eventDate": "2026-03-16T10:30:00Z",
              "applicationChannel": "InStore",
              "outletOrDealer": "CellShop Sandton City",
              "deviceIMEI": "356938035643809",
              "flagReason": "Third SIM application in 7 days. Velocity threshold exceeded."
            }
            """;

        var mnoResponse = await client.PostAsync(
            "/api/v1/mno-events",
            new StringContent(mnoJson, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, mnoResponse.StatusCode);
        var mnoBody = await mnoResponse.Content.ReadAsStringAsync();
        Assert.Contains("0821234567", mnoBody);
        Assert.Contains("NewSIMApplication", mnoBody);

        // 4.2.3 Anonymous Fraud Signal Response
        var signalsResponse = await client.GetAsync("/api/v1/fraud-signals?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, signalsResponse.StatusCode);

        var raw = await signalsResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("tenantId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Acme Holdings", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("0821234567", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("8501015800084", raw, StringComparison.Ordinal);

        var list = JsonSerializer.Deserialize<FraudSignalListResponse>(raw, TestHttpClientExtensions.JsonOptions);
        Assert.NotNull(list);
        Assert.True(list.TotalCount >= 1);
        Assert.Equal(1, list.Page);
        Assert.Equal(20, list.PageSize);
        Assert.NotEmpty(list.Signals);

        Assert.Contains(list.Signals, s =>
            s.SignalType is SignalType.HR_Alert or SignalType.MNO_Alert);

        var signal = list.Signals.First(s =>
            s.SignalType is SignalType.HR_Alert or SignalType.MNO_Alert);

        Assert.NotEqual(Guid.Empty, signal.SignalId);
        Assert.False(string.IsNullOrWhiteSpace(signal.IdNumberHash));
        Assert.DoesNotContain("8501015800084", signal.IdNumberHash);
        Assert.True(signal.OccurrenceCount >= 1);
        Assert.True(signal.AggregateRiskScore > 0);
        Assert.True(signal.IsActive);
    }
}
