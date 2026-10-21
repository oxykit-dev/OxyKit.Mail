using ErrorOr;

namespace OxyKit.Mail.Features.Email.Services;

/// <summary>
/// Abstraction for secure key storage operations.
/// </summary>
public interface IKeyStorage
{
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<ErrorOr<string>> ReadKeyAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// File system implementation of key storage.
/// Reads cryptographic keys from the file system.
/// </summary>
public class FileSystemKeyStorage : IKeyStorage
{
    /// <summary>
    /// Checks if a key file exists.
    /// </summary>
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        return Task.FromResult(File.Exists(path));
    }

    /// <summary>
    /// Reads the key content from a file.
    /// </summary>
    public async Task<ErrorOr<string>> ReadKeyAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        if (!File.Exists(path))
        {
            return Error.NotFound(
                code: "KeyStorage.FileNotFound",
                description: $"Key file not found: {path}"
            );
        }

        try
        {
            return await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Error.Failure(
                code: "KeyStorage.AccessDenied",
                description: $"Access denied to key file: {ex.Message}"
            );
        }
        catch (IOException ex)
        {
            return Error.Failure(
                code: "KeyStorage.ReadError",
                description: $"Failed to read key file: {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            return Error.Failure(
                code: "KeyStorage.UnknownError",
                description: $"Unexpected error reading key: {ex.Message}"
            );
        }
    }
}
