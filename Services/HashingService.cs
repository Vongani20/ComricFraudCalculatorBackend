using System.Security.Cryptography;
using System.Text;
using ComricFraudCalculatorBackend.Configuration;
using Microsoft.Extensions.Options;

namespace ComricFraudCalculatorBackend.Services;

public class HashingService(IOptions<PlatformOptions> platformOptions) : IHashingService
{
    public string HashIdNumber(string idNumber)
    {
        var normalized = idNumber.Trim();
        var salt = platformOptions.Value.Salt;
        var payload = string.IsNullOrEmpty(salt) ? normalized : salt + normalized;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(bytes);
    }
}
