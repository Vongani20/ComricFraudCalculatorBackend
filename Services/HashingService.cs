using System.Security.Cryptography;
using System.Text;

namespace ComricFraudCalculatorBackend.Services;

public class HashingService : IHashingService
{
    public string HashIdNumber(string idNumber)
    {
        var normalized = idNumber.Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexStringLower(bytes);
    }
}
