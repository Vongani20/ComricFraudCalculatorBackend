namespace ComricFraudCalculatorBackend.Models.Responses;

public record TokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string Scope);

public record IdCheckResponse(
    string IdNumberHash,
    bool MatchFound,
    IReadOnlyList<FraudSignalResponse> MatchingSignals);
