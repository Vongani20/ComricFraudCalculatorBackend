using System.ComponentModel.DataAnnotations;

namespace ComricFraudCalculatorBackend.Models.Requests;

public class IdCheckRequest
{
    [Required, MaxLength(20)]
    public string IdNumber { get; set; } = string.Empty;
}
