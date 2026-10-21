using Microsoft.EntityFrameworkCore;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Infrastructure.Repositories;

public class ApiKeyRepository(ApplicationDbContext context) : IApiKeyRepository
{
    public async Task<ApiKey?> GetByIdAsync(Guid apiKeyId, CancellationToken ct = default)
    {
        return await context
            .ApiKeys.Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == apiKeyId, ct);
    }

    public async Task<ApiKey?> GetByUserAndIdAsync(
        string userId,
        Guid apiKeyId,
        CancellationToken ct = default
    )
    {
        return await context
            .ApiKeys.Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == apiKeyId && k.UserId == userId, ct);
    }

    public async Task<List<ApiKey>> GetForUserAsync(string userId, CancellationToken ct = default)
    {
        return await context
            .ApiKeys.Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        context.ApiKeys.Add(apiKey);
        await context.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        context.ApiKeys.Remove(apiKey);
        await context.SaveChangesAsync(ct);
    }
}
