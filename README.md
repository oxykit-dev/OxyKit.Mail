# OxyKit.Mail

Email delivery API for the [OxyKit](https://oxykit.dev) suite: queue + retries, optional DKIM, JWT / API-key auth, dashboard metrics, SignalR.

**License:** MIT · **Version:** 0.1.0 (pre-1.0) · **Org:** [oxykit-dev](https://github.com/oxykit-dev)  
**Site:** [oxykit.dev](https://oxykit.dev) · **Config:** [ENVIRONMENT_SETTINGS.md](./ENVIRONMENT_SETTINGS.md) · **UI:** [oxykit-mail-web](https://github.com/oxykit-dev/oxykit-mail-web)

## Quick start

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download). From this folder, create a solution-root `.env`:

```env
DatabaseProvider=SQLite
ConnectionStrings__SQLiteConnection=Data Source=OxyKit.Mail.db
SmtpSettings__Host=smtp.example.com
SmtpSettings__Port=587
SmtpSettings__Username=your-username
SmtpSettings__Password=your-password
SmtpSettings__EnableSsl=true
SmtpSettings__AuthMode=Password
Jwt__Key=your-jwt-signing-key-at-least-32-chars
Jwt__Issuer=OxyKit.Mail
Jwt__Audience=OxyKit.Mail
```

```bash
dotnet run --project src/OxyKit.Mail/OxyKit.Mail.csproj
```

| URL | Purpose |
|-----|---------|
| `http://localhost:5026` | API |
| `http://localhost:5026/scalar` | OpenAPI UI (Development) |
| `http://localhost:5026/openapi/v1.json` | OpenAPI JSON (Development) |
| `http://localhost:5026/health` | Health (SMTP probe may be Unhealthy) |
| `http://localhost:5026/hubs/dashboard` | SignalR |

First run: `GET /api/v1/setup/needs-admin`, then `POST /api/v1/setup/create-admin` if needed.

**Smoke:** import [`hoppscotch/`](./hoppscotch/) collection + env → **OxyKit.Mail Local** → run folders in order.

**SMTP OAuth2** (M365 / Google): see [ENVIRONMENT_SETTINGS.md](./ENVIRONMENT_SETTINGS.md#smtp-oauth2).

```bash
dotnet test tests/OxyKit.Mail.Tests/OxyKit.Mail.Tests.csproj
dotnet test tests/OxyKit.Mail.Client.Tests/OxyKit.Mail.Client.Tests.csproj
```

## NuGet (Client)

```bash
dotnet add package OxyKit.Mail.Client
```

Also: `OxyKit.Mail.Contracts`. `MailClient.SendAsync` only — not a dashboard SDK. Auth: API key or Bearer.

Until packages are on nuget.org, reference the project from a sibling checkout:

```xml
<ProjectReference Include="..\OxyKit.Mail\src\OxyKit.Mail.Client\OxyKit.Mail.Client.csproj" />
```

**.NET Framework 2.0 / 4.5+:** do not use the Client package — call HTTP per the Framework HOW-TO on the docs site.

## Docs

| Doc | Role |
|-----|------|
| [AGENTS.md](./AGENTS.md) | Layout, auth, ErrorOr, change checklist |
| [ENVIRONMENT_SETTINGS.md](./ENVIRONMENT_SETTINGS.md) | `.env` / config / DKIM / OAuth2 |
| [hoppscotch/](./hoppscotch/) | Manual HTTP smoke |
| [CHANGELOG.md](./CHANGELOG.md) | `[Unreleased]` until first SemVer tag |
| `/scalar` · `/openapi/v1.json` | Live API shapes (Development) |
| [LICENSE](./LICENSE) | MIT |
