using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Live dashboard updates. Sender is send-only and must not join this hub.
/// </summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.User}")]
public class DashboardHub : Hub { }
