using ComricFraudCalculatorBackend.Authorization;
using ComricFraudCalculatorBackend.Models.Responses;
using ComricFraudCalculatorBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComricFraudCalculatorBackend.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Policy = AuthPolicies.DashboardRead)]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsResponse>> GetStats(CancellationToken ct = default)
    {
        return Ok(await dashboardService.GetStatsAsync(ct));
    }

    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewResponse>> GetOverview(CancellationToken ct = default)
    {
        return Ok(await dashboardService.GetOverviewAsync(ct));
    }
}
