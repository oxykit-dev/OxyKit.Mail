namespace OxyKit.Mail.Features.Dashboard;

public class EmailsPerDayRequest
{
    public DateTime? Since { get; set; } = null;
    public string? TimeZoneId { get; set; } = null;
}
