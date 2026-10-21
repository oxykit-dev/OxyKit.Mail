using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Infrastructure.Repositories;

/// <summary>
/// Repository contract for API key persistence.
/// </summary>
public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(Guid apiKeyId, CancellationToken ct = default);

    Task<ApiKey?> GetByUserAndIdAsync(string userId, Guid apiKeyId, CancellationToken ct = default);

    Task<List<ApiKey>> GetForUserAsync(string userId, CancellationToken ct = default);

    Task AddAsync(ApiKey apiKey, CancellationToken ct = default);

    Task RemoveAsync(ApiKey apiKey, CancellationToken ct = default);
}
