using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace OxyKit.Mail.Infrastructure.Entities;

/// <summary>
/// Represents an API key for authentication.
/// </summary>
public class ApiKey : EntityBase
{
    [Required]
    public string UserId { get; set; } = default!;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = default!;

    [Required]
    public string Hash { get; set; } = default!;

    [Required]
    public string Salt { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string Label { get; set; } = default!;

    public DateTime? LastUsedAt { get; set; }

    public static string HashApiKey(string apiKey, string salt)
    {
        var combined = Encoding.UTF8.GetBytes(apiKey + salt);
        var hash = SHA256.HashData(combined);
        return Convert.ToBase64String(hash);
    }

    public static (ApiKey apiKey, string plainApiKey) Create(string userId, string label)
    {
        var id = Guid.NewGuid();
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var randomPart = Convert.ToBase64String(bytes);
        var saltBytes = new byte[16];
        RandomNumberGenerator.Fill(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);
        var apiKey = $"{id}.{randomPart}";
        var hash = HashApiKey(apiKey, salt);

        var entity = new ApiKey
        {
            Id = id,
            UserId = userId,
            Label = label,
            Hash = hash,
            Salt = salt,
            CreatedAt = DateTime.UtcNow,
        };
        return (entity, apiKey);
    }

    public static async Task<ApiKey?> FindByApiKeyAsync(string apiKey, DbSet<ApiKey> apiKeys)
    {
        var parts = apiKey.Split('.', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var id))
            return null;

        var entity = await apiKeys.FirstOrDefaultAsync(k => k.Id == id);
        if (entity == null)
            return null;

        var hash = HashApiKey(apiKey, entity.Salt);
        return entity.Hash == hash ? entity : null;
    }
}
