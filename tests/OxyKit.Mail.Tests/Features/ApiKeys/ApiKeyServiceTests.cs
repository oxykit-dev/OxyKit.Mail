using ErrorOr;
using FluentAssertions;
using Moq;
using OxyKit.Mail.Features.ApiKeys.Services;
using OxyKit.Mail.Infrastructure.Entities;
using OxyKit.Mail.Infrastructure.Repositories;
using OxyKit.Mail.Tests.Utilities;

namespace OxyKit.Mail.Tests.Features.ApiKeys;

/// <summary>
/// Unit tests for ApiKeyService.
/// </summary>
public class ApiKeyServiceTests
{
    private Mock<IApiKeyRepository> _repoMock = null!;
    private ApiKeyService _sut = null!;

    public ApiKeyServiceTests()
    {
        _repoMock = new Mock<IApiKeyRepository>();
        _sut = new ApiKeyService(_repoMock.Object);
    }

    [Fact]
    public async Task CreateApiKeyAsync_WithEmptyLabel_ReturnsValidationError()
    {
        var result = await _sut.CreateApiKeyAsync("user-1", "   ");

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Code.Should().Be("LabelRequired");
        _repoMock.Verify(
            x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetApiKeyAsync_WhenNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repoMock
            .Setup(x => x.GetByUserAndIdAsync("user-1", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiKey?)null);

        var result = await _sut.GetApiKeyAsync("user-1", id);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task RevokeApiKeyAsync_WhenNotFound_ReturnsNotFoundError()
    {
        var id = Guid.NewGuid();
        _repoMock
            .Setup(x => x.GetByUserAndIdAsync("user-1", id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiKey?)null);

        var result = await _sut.RevokeApiKeyAsync(id, "user-1");

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        _repoMock.Verify(
            x => x.RemoveAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task GetApiKeysForUserAsync_ReturnsKeysFromRepository()
    {
        var (key1, _) = TestDataBuilder.CreateApiKey("user-1");
        var (key2, _) = TestDataBuilder.CreateApiKey("user-1", k => k.Label = "Second");
        _repoMock
            .Setup(x => x.GetForUserAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([key1, key2]);

        var result = await _sut.GetApiKeysForUserAsync("user-1");

        result.Should().HaveCount(2);
        result.Should().Contain(key1);
        result.Should().Contain(key2);
    }
}
