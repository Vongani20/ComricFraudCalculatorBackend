using System.ComponentModel.DataAnnotations;

namespace ComricFraudCalculatorBackend.Models.Requests;

public class TokenRequest
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;

    public string Scope { get; set; } = "Events.Read Events.Write Signals.Read";
}
