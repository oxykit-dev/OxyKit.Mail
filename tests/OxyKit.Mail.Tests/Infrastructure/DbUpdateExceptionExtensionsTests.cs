using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Tests.Infrastructure;

public class DbUpdateExceptionExtensionsTests
{
    [Fact]
    public void IsUniqueConstraintViolation_SqliteUniqueMessage_ReturnsTrue()
    {
        var inner = new Exception("SQLite Error 19: 'UNIQUE constraint failed: Messages.IdempotencyKey'.");
        var ex = new DbUpdateException("save failed", inner);

        ex.IsUniqueConstraintViolation().Should().BeTrue();
    }

    [Fact]
    public void IsUniqueConstraintViolation_UnrelatedFailure_ReturnsFalse()
    {
        var inner = new Exception("disk I/O error");
        var ex = new DbUpdateException("save failed", inner);

        ex.IsUniqueConstraintViolation().Should().BeFalse();
    }
}
