namespace S3.Client.Models.Security;

public interface IS3Credential
{
    string AccessKeyId			{ get; }

    string SecretAccessKey		{ get; }

    string? SecurityToken		{ get; }

    bool ShouldRenew			{ get; }

    Task<bool> Renew();
}