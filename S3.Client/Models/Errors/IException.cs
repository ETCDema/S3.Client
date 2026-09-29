namespace S3.Client.Models.Errors;

public interface IException
{
    bool IsTransient { get; }
}