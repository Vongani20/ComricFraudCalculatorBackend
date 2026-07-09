using ComricFraudCalculatorBackend.Authorization;
using ComricFraudCalculatorBackend.Models.Responses;
using ComricFraudCalculatorBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComricFraudCalculatorBackend.Controllers;

[ApiController]
[Route("api/v1/fraud-signals")]
[Authorize(Policy = AuthPolicies.SignalsRead)]
public class FraudSignalsController(IFraudSignalService signalService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<FraudSignalListResponse>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? activeOnly = true,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await signalService.ListAsync(page, pageSize, activeOnly, ct));
    }

    [HttpGet("{idHash}")]
    public async Task<ActionResult<FraudSignalResponse>> GetByHash(string idHash, CancellationToken ct = default)
    {
        var signal = await signalService.GetByHashAsync(idHash, ct);
        return signal is null ? NotFound() : Ok(signal);
    }
}
