namespace OxyKit.Mail.Features.ApiKeys;

public record CreateApiKeyResponse(
    Guid Id,
    string UserId,
    string Label,
    DateTime CreatedAt,
    DateTime? LastUsedAt,
    string PlainApiKey
);
