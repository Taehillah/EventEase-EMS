namespace EventEase.EMS.Services;

public class ConfigAdminCredentialValidator(IConfiguration configuration) : IAdminCredentialValidator
{
    public bool IsValid(string email, string password)
    {
        var configEmail = configuration["AdminUser:Email"];
        var configPassword = configuration["AdminUser:Password"];

        return !string.IsNullOrWhiteSpace(configEmail) && !string.IsNullOrWhiteSpace(configPassword)
            && string.Equals(email.Trim(), configEmail, StringComparison.OrdinalIgnoreCase)
            && string.Equals(password, configPassword, StringComparison.Ordinal);
    }
}
