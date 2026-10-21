using ErrorOr;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;
using OxyKit.Mail.Infrastructure.Entities;
using OxyKit.Mail.Tests.Utilities;

namespace OxyKit.Mail.Tests.Features.Identity;

/// <summary>
/// Unit tests for UserService - critical authentication and user management service.
/// Tests cover user retrieval, creation, updates, deletion, password management, and validation.
/// </summary>
public class UserServiceTests
{
    private Mock<UserManager<ApplicationUser>> _userManagerMock = null!;
    private Mock<ILogger<UserService>> _loggerMock = null!;
    private UserService _sut = null!;

    public UserServiceTests()
    {
        _userManagerMock = MockHelpers.MockUserManager<ApplicationUser>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _sut = new UserService(_userManagerMock.Object, _loggerMock.Object);
    }

    #region GetUserByIdAsync

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        var userId = "user-123";
        var expectedUser = new ApplicationUser { Id = userId, UserName = "testuser" };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(expectedUser);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expectedUser);
        result.Value.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNotFoundError()
    {
        // Arrange
        var userId = "non-existent";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.GetUserByIdAsync(userId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Description.Should().Contain(userId);
    }

    #endregion

    #region GetUserByUserNameAsync

    [Fact]
    public async Task GetUserByUserNameAsync_WithValidUsername_ShouldReturnUser()
    {
        // Arrange
        var userName = "testuser";
        var expectedUser = new ApplicationUser { Id = "user-123", UserName = userName };

        _userManagerMock.Setup(x => x.FindByNameAsync(userName)).ReturnsAsync(expectedUser);

        // Act
        var result = await _sut.GetUserByUserNameAsync(userName);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expectedUser);
        result.Value.UserName.Should().Be(userName);
    }

    [Fact]
    public async Task GetUserByUserNameAsync_WithInvalidUsername_ShouldReturnNotFoundError()
    {
        // Arrange
        var userName = "nonexistent";

        _userManagerMock
            .Setup(x => x.FindByNameAsync(userName))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.GetUserByUserNameAsync(userName);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Description.Should().Contain(userName);
    }

    #endregion

    #region GetUserByEmailAsync

    [Fact]
    public async Task GetUserByEmailAsync_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        var email = "test@example.com";
        var expectedUser = new ApplicationUser { Id = "user-123", Email = email };

        _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(expectedUser);

        // Act
        var result = await _sut.GetUserByEmailAsync(email);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expectedUser);
        result.Value.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithInvalidEmail_ShouldReturnNotFoundError()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _userManagerMock.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.GetUserByEmailAsync(email);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Description.Should().Contain(email);
    }

    #endregion

    #region GetUserByIdentifierAsync

    [Fact]
    public async Task GetUserByIdentifierAsync_WithUsername_ShouldReturnUser()
    {
        // Arrange
        var identifier = "testuser";
        var expectedUser = new ApplicationUser { Id = "user-123", UserName = identifier };

        _userManagerMock.Setup(x => x.FindByNameAsync(identifier)).ReturnsAsync(expectedUser);

        // Act
        var result = await _sut.GetUserByIdentifierAsync(identifier);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expectedUser);
    }

    [Fact]
    public async Task GetUserByIdentifierAsync_WithEmail_ShouldReturnUser()
    {
        // Arrange
        var identifier = "test@example.com";
        var expectedUser = new ApplicationUser { Id = "user-123", Email = identifier };

        _userManagerMock
            .Setup(x => x.FindByNameAsync(identifier))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock.Setup(x => x.FindByEmailAsync(identifier)).ReturnsAsync(expectedUser);

        // Act
        var result = await _sut.GetUserByIdentifierAsync(identifier);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expectedUser);
        result.Value.Email.Should().Be(identifier);
    }

    [Fact]
    public async Task GetUserByIdentifierAsync_WithInvalidIdentifier_ShouldReturnNotFoundError()
    {
        // Arrange
        var identifier = "nonexistent";

        _userManagerMock
            .Setup(x => x.FindByNameAsync(identifier))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(identifier))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.GetUserByIdentifierAsync(identifier);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region CreateUserAsync

    [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var userName = "newuser";
        var email = "new@example.com";
        var password = "Password123!";

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.CreateUserAsync(userName, email, password);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Succeeded.Should().BeTrue();

        _userManagerMock.Verify(
            x =>
                x.CreateAsync(
                    It.Is<ApplicationUser>(u => u.UserName == userName && u.Email == email),
                    password
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task CreateUserAsync_WithInvalidData_ShouldReturnValidationError()
    {
        // Arrange
        var userName = "newuser";
        var email = "invalid-email";
        var password = "weak";
        var errors = new[]
        {
            new IdentityError { Description = "Invalid email format" },
            new IdentityError { Description = "Password too weak" },
        };

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _sut.CreateUserAsync(userName, email, password);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().Contain("Invalid email format");
        result.FirstError.Description.Should().Contain("Password too weak");
    }

    #endregion

    #region UpdateUserAsync

    [Fact]
    public async Task UpdateUserAsync_WithValidUser_ShouldSucceed()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };

        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.UpdateUserAsync(user);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Succeeded.Should().BeTrue();

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WithFailure_ShouldReturnValidationError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        var errors = new[] { new IdentityError { Description = "Username already taken" } };

        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _sut.UpdateUserAsync(user);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().Contain("Username already taken");
    }

    #endregion

    #region DeleteUserAsync

    [Fact]
    public async Task DeleteUserAsync_WithValidUser_ShouldSucceed()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };

        _userManagerMock.Setup(x => x.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.DeleteUserAsync(user);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Succeeded.Should().BeTrue();

        _userManagerMock.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_WithFailure_ShouldReturnValidationError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        var errors = new[] { new IdentityError { Description = "Cannot delete last admin user" } };

        _userManagerMock
            .Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _sut.DeleteUserAsync(user);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().Contain("Cannot delete last admin user");
    }

    #endregion

    #region SetUserPasswordAsync

    [Fact]
    public async Task SetUserPasswordAsync_WithValidPassword_ShouldSucceed()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        var newPassword = "NewPassword123!";
        var resetToken = "reset-token-123";

        _userManagerMock
            .Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);

        _userManagerMock
            .Setup(x => x.ResetPasswordAsync(user, resetToken, newPassword))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.SetUserPasswordAsync(user, newPassword);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Succeeded.Should().BeTrue();

        _userManagerMock.Verify(x => x.GeneratePasswordResetTokenAsync(user), Times.Once);
        _userManagerMock.Verify(
            x => x.ResetPasswordAsync(user, resetToken, newPassword),
            Times.Once
        );
    }

    [Fact]
    public async Task SetUserPasswordAsync_WithWeakPassword_ShouldReturnValidationError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        var newPassword = "weak";
        var resetToken = "reset-token-123";
        var errors = new[]
        {
            new IdentityError { Description = "Password must be at least 8 characters" },
        };

        _userManagerMock
            .Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);

        _userManagerMock
            .Setup(x => x.ResetPasswordAsync(user, resetToken, newPassword))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _sut.SetUserPasswordAsync(user, newPassword);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().Contain("at least 8 characters");
    }

    #endregion

    #region ValidatePasswordAsync

    [Fact]
    public async Task ValidatePasswordAsync_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        var password = "CorrectPassword123!";

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, password)).ReturnsAsync(true);

        // Act
        var result = await _sut.ValidatePasswordAsync(user, password);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePasswordAsync_WithIncorrectPassword_ShouldReturnValidationError()
    {
        // Arrange
        var user = new ApplicationUser { Id = "user-123", UserName = "testuser" };
        var password = "WrongPassword";

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, password)).ReturnsAsync(false);

        // Act
        var result = await _sut.ValidatePasswordAsync(user, password);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        result.FirstError.Description.Should().Contain("Invalid password");
    }

    #endregion

    #region AdminUserExistsAsync

    [Fact]
    public async Task AdminUserExistsAsync_WhenAdminsExist_ShouldReturnTrue()
    {
        // Arrange
        var adminUsers = new List<ApplicationUser>
        {
            new() { Id = "admin-1", UserName = "admin1" },
            new() { Id = "admin-2", UserName = "admin2" },
        };

        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(AppRoles.Admin)).ReturnsAsync(adminUsers);

        // Act
        var result = await _sut.AdminUserExistsAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AdminUserExistsAsync_WhenNoAdmins_ShouldReturnFalse()
    {
        // Arrange
        _userManagerMock.Setup(x => x.GetUsersInRoleAsync(AppRoles.Admin)).ReturnsAsync([]);

        // Act
        var result = await _sut.AdminUserExistsAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateUserAsync

    [Fact]
    public async Task ValidateUserAsync_WithValidCredentials_ShouldReturnUser()
    {
        // Arrange
        var identifier = "testuser";
        var password = "Password123!";
        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = identifier,
            LockoutEnabled = false,
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(identifier)).ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, password)).ReturnsAsync(true);

        // Act
        var result = await _sut.ValidateUserAsync(identifier, password);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(user);
    }

    [Fact]
    public async Task ValidateUserAsync_WithInvalidPassword_ShouldReturnValidationError()
    {
        // Arrange
        var identifier = "testuser";
        var password = "WrongPassword";
        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = identifier,
            LockoutEnabled = false,
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(identifier)).ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, password)).ReturnsAsync(false);

        // Act
        var result = await _sut.ValidateUserAsync(identifier, password);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task ValidateUserAsync_WithLockedOutUser_ShouldReturnForbiddenError()
    {
        // Arrange
        var identifier = "testuser";
        var password = "Password123!";
        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = identifier,
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30), // Locked for 30 more minutes
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(identifier)).ReturnsAsync(user);

        // Act
        var result = await _sut.ValidateUserAsync(identifier, password);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        result.FirstError.Description.Should().Contain("locked out");
    }

    [Fact]
    public async Task ValidateUserAsync_WithNonExistentUser_ShouldReturnNotFoundError()
    {
        // Arrange
        var identifier = "nonexistent";
        var password = "Password123!";

        _userManagerMock
            .Setup(x => x.FindByNameAsync(identifier))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(identifier))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.ValidateUserAsync(identifier, password);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ValidateUserAsync_WithExpiredLockout_ShouldAllowLogin()
    {
        // Arrange
        var identifier = "testuser";
        var password = "Password123!";
        var user = new ApplicationUser
        {
            Id = "user-123",
            UserName = identifier,
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-5), // Lockout expired 5 minutes ago
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(identifier)).ReturnsAsync(user);

        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, password)).ReturnsAsync(true);

        // Act
        var result = await _sut.ValidateUserAsync(identifier, password);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(user);
    }

    #endregion
}
