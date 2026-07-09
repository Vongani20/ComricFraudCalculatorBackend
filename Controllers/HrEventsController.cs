using ComricFraudCalculatorBackend.Authorization;
using ComricFraudCalculatorBackend.Models.Requests;
using ComricFraudCalculatorBackend.Models.Responses;
using ComricFraudCalculatorBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComricFraudCalculatorBackend.Controllers;

[ApiController]
[Route("api/v1/hr-events")]
[Authorize(Policy = AuthPolicies.EventsRead)]
public class HrEventsController(IHrEventService hrEventService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<HrEventResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await hrEventService.ListAsync(page, pageSize, ct));
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.EventsWrite)]
    public async Task<ActionResult<HrEventResponse>> Submit(
        [FromBody] SubmitHrEventRequest request,
        CancellationToken ct = default)
    {
        var result = await hrEventService.SubmitAsync(request, ct);
        return CreatedAtAction(nameof(List), new { page = 1 }, result);
    }
}
