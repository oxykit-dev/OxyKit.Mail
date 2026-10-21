namespace OxyKit.Mail.Features.ApiKeys;

public record ApiKeyResponse(Guid Id, string Label, DateTime CreatedAt, DateTime? LastUsedAt);
