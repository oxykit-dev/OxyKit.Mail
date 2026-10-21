namespace OxyKit.Mail.Features.Identity;

/// <summary>
/// Request model for user authentication.
/// </summary>
/// <param name="Identifier">User email or username.</param>
/// <param name="Password">User password.</param>
/// <param name="DeviceId">Unique device identifier for refresh token tracking.</param>
public record LoginRequest(string Identifier, string Password, string DeviceId);
