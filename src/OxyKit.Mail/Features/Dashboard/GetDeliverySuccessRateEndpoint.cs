using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Dashboard.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Dashboard;

public class GetDeliverySuccessRateEndpoint(IDashboardService dashboardService)
    : MailEndpointWithoutRequest<DeliverySuccessRateResponse>
{
    public override void Configure()
    {
        Get("/dashboard/delivery-success-rate");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.User);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await dashboardService.GetDeliverySuccessRateAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
