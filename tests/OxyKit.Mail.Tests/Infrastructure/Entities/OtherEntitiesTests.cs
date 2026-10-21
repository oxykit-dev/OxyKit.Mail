using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Tests.Infrastructure.Entities;

public class MessageLogTests
{
    [Fact]
    public void MessageLog_DefaultValues_AreCorrect()
    {
        var log = new MessageLog();
        Assert.Equal(Guid.Empty, log.MessageId);
        Assert.Null(log.ErrorMessage);
        Assert.Equal(MessageStatus.Queued, log.Status); // Default from MessageStatus enum
        Assert.True((DateTime.UtcNow - log.LoggedAt).TotalSeconds < 5);
    }
}

public class ApiKeyTests
{
    [Fact]
    public void ApiKey_Create_SetsPropertiesCorrectly()
    {
        var (apiKey, plain) = ApiKey.Create("user1", "label1");
        Assert.Equal("user1", apiKey.UserId);
        Assert.Equal("label1", apiKey.Label);
        Assert.NotNull(apiKey.Hash);
        Assert.NotNull(apiKey.Salt);
        Assert.NotEqual(Guid.Empty, apiKey.Id);
        Assert.True((DateTime.UtcNow - apiKey.CreatedAt).TotalSeconds < 5);
        Assert.False(string.IsNullOrWhiteSpace(plain));
    }

    [Fact]
    public void HashApiKey_ProducesConsistentHash()
    {
        var hash1 = ApiKey.HashApiKey("key", "salt");
        var hash2 = ApiKey.HashApiKey("key", "salt");
        Assert.Equal(hash1, hash2);
    }
}

public class RefreshTokenTests
{
    [Fact]
    public void RefreshToken_DefaultValues_AreCorrect()
    {
        var token = new RefreshToken();
        Assert.Equal(string.Empty, token.Token);
        Assert.Equal(string.Empty, token.DeviceId);
        Assert.Equal(string.Empty, token.UserId);
        Assert.False(token.Revoked);
        Assert.Null(token.User);
    }
}

public class AttachmentTests
{
    [Fact]
    public void Attachment_DefaultValues_AreCorrect()
    {
        var att = new Attachment();
        Assert.Equal(string.Empty, att.FileName);
        Assert.Equal(string.Empty, att.ContentType);
        Assert.NotNull(att.Content);
        Assert.Empty(att.Content);
        Assert.Equal(0, att.GetSize());
    }

    [Fact]
    public void IsValid_ValidAttachment_ReturnsTrue()
    {
        var att = new Attachment
        {
            FileName = "file.txt",
            Content = [1],
            ContentType = "text/plain",
        };
        Assert.True(att.IsValid());
    }

    [Fact]
    public void IsValid_InvalidAttachment_ReturnsFalse()
    {
        var att = new Attachment
        {
            FileName = "",
            Content = [],
            ContentType = "",
        };
        Assert.False(att.IsValid());
    }

    [Fact]
    public void IsTypeAllowed_AllowedType_ReturnsTrue()
    {
        var att = new Attachment
        {
            FileName = "file.txt",
            Content = [1],
            ContentType = "text/plain",
        };
        Assert.True(att.IsTypeAllowed(["text/plain"]));
    }

    [Fact]
    public void IsTypeAllowed_NotAllowedType_ReturnsFalse()
    {
        var att = new Attachment
        {
            FileName = "file.txt",
            Content = [1],
            ContentType = "text/plain",
        };
        Assert.False(att.IsTypeAllowed(["application/pdf"]));
    }
}

public class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_DefaultValues_AreCorrect()
    {
        var user = new ApplicationUser();
        Assert.NotNull(user.ApiKeys);
        Assert.Empty(user.ApiKeys);
        Assert.Null(user.Avatar);
        Assert.Null(user.AvatarThumbnail);
    }
}
