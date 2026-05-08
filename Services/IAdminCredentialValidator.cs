namespace EventEase.EMS.Services;

public interface IAdminCredentialValidator
{
    bool IsValid(string email, string password);
}
