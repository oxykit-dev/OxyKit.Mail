using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Tests.Utilities;

/// <summary>
/// Utility class for creating test data objects.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a test Message with default values.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>A configured Message instance.</returns>
    public static Message CreateMessage(Action<Message>? configure = null)
    {
        var message = new Message
        {
            Id = Guid.NewGuid(),
            From = "test@example.com",
            To = "recipient@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            Status = MessageStatus.Queued,
            IsHtml = true,
            AttemptCount = 0,
            MaxAttempts = 3,
            CreatedAt = DateTime.UtcNow,
        };
        configure?.Invoke(message);
        return message;
    }

    /// <summary>
    /// Creates a test ApplicationUser with default values.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>A configured ApplicationUser instance.</returns>
    public static ApplicationUser CreateUser(Action<ApplicationUser>? configure = null)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"testuser_{Guid.NewGuid():N}",
            Email = $"test_{Guid.NewGuid():N}@example.com",
            EmailConfirmed = true,
            NormalizedUserName = $"TESTUSER_{Guid.NewGuid():N}",
            NormalizedEmail = $"TEST_{Guid.NewGuid():N}@EXAMPLE.COM",
        };
        configure?.Invoke(user);
        return user;
    }

    /// <summary>
    /// Creates a test Attachment with default values.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>A configured Attachment instance.</returns>
    public static Attachment CreateAttachment(Action<Attachment>? configure = null)
    {
        var attachment = new Attachment
        {
            FileName = "test.txt",
            Content = [1, 2, 3, 4, 5],
            ContentType = "text/plain",
        };
        configure?.Invoke(attachment);
        return attachment;
    }

    /// <summary>
    /// Creates a test ApiKey with default values.
    /// </summary>
    /// <param name="userId">The user ID for the API key.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>A configured ApiKey instance and plain key.</returns>
    public static (ApiKey apiKey, string plainKey) CreateApiKey(
        string userId,
        Action<ApiKey>? configure = null
    )
    {
        var (apiKey, plainKey) = ApiKey.Create(userId, "Test Key");
        configure?.Invoke(apiKey);
        return (apiKey, plainKey);
    }

    /// <summary>
    /// Creates a test MessageLog with default values.
    /// </summary>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>A configured MessageLog instance.</returns>
    public static MessageLog CreateMessageLog(Action<MessageLog>? configure = null)
    {
        var message = CreateMessage(); // Create associated message
        var log = new MessageLog
        {
            Id = Guid.NewGuid(),
            MessageId = message.Id,
            Message = message,
            Status = MessageStatus.Sent,
            LoggedAt = DateTime.UtcNow,
        };
        configure?.Invoke(log);
        return log;
    }
}
