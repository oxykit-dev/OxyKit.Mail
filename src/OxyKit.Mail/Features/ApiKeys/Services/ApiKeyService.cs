using ErrorOr;
using OxyKit.Mail.Infrastructure.Entities;
using OxyKit.Mail.Infrastructure.Repositories;

namespace OxyKit.Mail.Features.ApiKeys.Services;

/// <summary>
/// Interface for API key management operations.
/// </summary>
public interface IApiKeyService
{
    Task<ErrorOr<CreateApiKeyResponse>> CreateApiKeyAsync(string userId, string label);
    Task<ErrorOr<ApiKey>> GetApiKeyAsync(Guid apiKeyId);
    Task<ErrorOr<ApiKey>> GetApiKeyAsync(string userId, Guid apiKeyId);
    Task<List<ApiKey>> GetApiKeysForUserAsync(string userId);
    Task<ErrorOr<Success>> RevokeApiKeyAsync(Guid apiKeyId, string userId);
}

/// <summary>
/// Service for API key management operations.
/// </summary>
public class ApiKeyService(IApiKeyRepository apiKeyRepository) : IApiKeyService
{
    /// <summary>
    /// Creates a new API key for a user.
    /// </summary>
    public async Task<ErrorOr<CreateApiKeyResponse>> CreateApiKeyAsync(string userId, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return Error.Validation(code: "LabelRequired", description: "Label is required.");

        var (entity, plainApiKey) = ApiKey.Create(userId, label);
        await apiKeyRepository.AddAsync(entity);
        var result = await apiKeyRepository.GetByIdAsync(entity.Id);
        if (result is null)
            return Error.NotFound(description: "API key not found after creation.");
        return new CreateApiKeyResponse(
            result.Id,
            result.UserId,
            result.Label,
            result.CreatedAt,
            result.LastUsedAt,
            plainApiKey
        );
    }

    /// <summary>
    /// Gets an API key by its ID.
    /// </summary>
    public async Task<ErrorOr<ApiKey>> GetApiKeyAsync(Guid apiKeyId)
    {
        return await apiKeyRepository.GetByIdAsync(apiKeyId)
            ?? (ErrorOr<ApiKey>)Error.NotFound(description: "API key not found.");
    }

    /// <summary>
    /// Gets an API key by user ID and API key ID.
    /// </summary>
    public async Task<ErrorOr<ApiKey>> GetApiKeyAsync(string userId, Guid apiKeyId)
    {
        return await apiKeyRepository.GetByUserAndIdAsync(userId, apiKeyId)
            ?? (ErrorOr<ApiKey>)Error.NotFound(description: "API key not found.");
    }

    /// <summary>
    /// Gets all API keys for a specific user.
    /// </summary>
    public async Task<List<ApiKey>> GetApiKeysForUserAsync(string userId)
    {
        return await apiKeyRepository.GetForUserAsync(userId);
    }

    /// <summary>
    /// Revokes (deletes) an API key.
    /// </summary>
    public async Task<ErrorOr<Success>> RevokeApiKeyAsync(Guid apiKeyId, string userId)
    {
        var key = await apiKeyRepository.GetByUserAndIdAsync(userId, apiKeyId);
        if (key == null)
            return Error.NotFound(description: "API key not found.");
        await apiKeyRepository.RemoveAsync(key);
        return Result.Success;
    }
}
