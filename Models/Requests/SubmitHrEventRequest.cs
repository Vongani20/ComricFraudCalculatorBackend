using System.ComponentModel.DataAnnotations;
using ComricFraudCalculatorBackend.Enums;

namespace ComricFraudCalculatorBackend.Models.Requests;

public class SubmitHrEventRequest
{
    [Required, MaxLength(20)]
    public string IdNumber { get; set; } = string.Empty;

    [Required]
    public HrEventType EventType { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(200)]
    public string EmployerName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? EmployeeNumber { get; set; }

    [Required]
    public VerificationStatus VerificationStatus { get; set; }

    public string? Notes { get; set; }
}
