namespace ComricFraudCalculatorBackend.Services;

public interface IHashingService
{
    string HashIdNumber(string idNumber);
}
