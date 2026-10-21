namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Response containing delivery success rate statistics.
/// </summary>
public record DeliverySuccessRateResponse(double SuccessRate, int TotalSent, int TotalFailed);
