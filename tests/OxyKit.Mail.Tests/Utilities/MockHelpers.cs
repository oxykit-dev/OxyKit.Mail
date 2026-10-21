using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Tests.Utilities;

/// <summary>
/// Utility class for creating mock objects for testing.
/// </summary>
public static class MockHelpers
{
    /// <summary>
    /// Creates a mock UserManager for testing (generic version).
    /// </summary>
    public static Mock<UserManager<TUser>> MockUserManager<TUser>()
        where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        var optionsMock = new Mock<IOptions<IdentityOptions>>();
        var passwordHasher = new Mock<IPasswordHasher<TUser>>();
        var userValidators = new List<IUserValidator<TUser>>();
        var passwordValidators = new List<IPasswordValidator<TUser>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new Mock<IdentityErrorDescriber>();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<UserManager<TUser>>>();

        return new Mock<UserManager<TUser>>(
            store.Object,
            optionsMock.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors.Object,
            services.Object,
            logger.Object
        );
    }

    /// <summary>
    /// Creates a mock UserManager for testing.
    /// </summary>
    /// <returns>A configured Mock of UserManager.</returns>
    public static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
    {
        return MockUserManager<ApplicationUser>();
    }

    /// <summary>
    /// Creates a mock SignInManager for testing.
    /// </summary>
    /// <param name="userManager">The UserManager mock to use.</param>
    /// <returns>A configured Mock of SignInManager.</returns>
    public static Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock(
        Mock<UserManager<ApplicationUser>> userManager
    )
    {
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var options = new Mock<IOptions<IdentityOptions>>();
        var logger = new Mock<ILogger<SignInManager<ApplicationUser>>>();
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        var confirmation = new Mock<IUserConfirmation<ApplicationUser>>();

        return new Mock<SignInManager<ApplicationUser>>(
            userManager.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            options.Object,
            logger.Object,
            schemes.Object,
            confirmation.Object
        );
    }

    /// <summary>
    /// Creates a mock RoleManager for testing.
    /// </summary>
    /// <returns>A configured Mock of RoleManager.</returns>
    public static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
    {
        var store = new Mock<IRoleStore<IdentityRole>>();
        var roleValidators = new List<IRoleValidator<IdentityRole>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new Mock<IdentityErrorDescriber>();
        var logger = new Mock<ILogger<RoleManager<IdentityRole>>>();

        return new Mock<RoleManager<IdentityRole>>(
            store.Object,
            roleValidators,
            keyNormalizer.Object,
            errors.Object,
            logger.Object
        );
    }
}

/// <summary>
/// Extension methods for creating mock DbSets.
/// </summary>
public static class MockDbSetExtensions
{
    /// <summary>
    /// Builds a mock DbSet from an IQueryable.
    /// </summary>
    public static Mock<DbSet<T>> BuildMockDbSet<T>(this IQueryable<T> data)
        where T : class
    {
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

        return mockSet;
    }
}
