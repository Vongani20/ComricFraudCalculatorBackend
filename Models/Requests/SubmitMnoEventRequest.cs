using System.ComponentModel.DataAnnotations;
using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Models.Requests;

public class SubmitMnoEventRequest
{
    [Required, MaxLength(20)]
    public string IdNumber { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Msisdn { get; set; } = string.Empty;

    [Required]
    public MnoEventType EventType { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required]
    public ApplicationChannel ApplicationChannel { get; set; }

    [Required, MaxLength(200)]
    public string OutletOrDealer { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DeviceImei { get; set; }

    public string? FlagReason { get; set; }
}
