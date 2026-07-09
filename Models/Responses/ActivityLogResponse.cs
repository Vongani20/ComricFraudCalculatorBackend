namespace ComricFraudCalculatorBackend.Models.Responses;

public record ActivityLogResponse(
    Guid ActivityLogId,
    string Action,
    string Endpoint,
    string HttpMethod,
    int StatusCode,
    string? ClientIp,
    DateTime CreatedAt);

public record ActivityLogListResponse(
    IReadOnlyList<ActivityLogResponse> Entries,
    int TotalCount,
    int Page,
    int PageSize);
