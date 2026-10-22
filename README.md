# OxyKit.Mail

[![CI](https://github.com/oxykit-dev/OxyKit.Mail/actions/workflows/ci.yml/badge.svg)](https://github.com/oxykit-dev/OxyKit.Mail/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/OxyKit.Mail.Client.svg?label=OxyKit.Mail.Client)](https://www.nuget.org/packages/OxyKit.Mail.Client)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](./LICENSE)

Email **delivery API** for the [OxyKit](https://oxykit.dev) suite: queue + retries, optional DKIM, JWT / API-key auth, dashboard metrics, SignalR.

**License:** MIT · **Org:** [oxykit-dev](https://github.com/oxykit-dev) · **Site:** [oxykit.dev](https://oxykit.dev)  
**Releases:** [app zips](https://github.com/oxykit-dev/OxyKit.Mail/releases) · **Client:** [`OxyKit.Mail.Client`](https://www.nuget.org/packages/OxyKit.Mail.Client)  
**Related:** [ENVIRONMENT_SETTINGS.md](./ENVIRONMENT_SETTINGS.md) · UI [oxykit-mail-web](https://github.com/oxykit-dev/oxykit-mail-web)

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

| URL / header | Purpose |
|--------------|---------|
| `http://localhost:5026` | API |
| `X-Api-Key` or Bearer JWT | Auth (create a sender key after bootstrap) |
| `GET /health` | Health (SMTP probe may be Unhealthy) |
| `/scalar`, `/openapi/v1.json` | OpenAPI UI + JSON (Development only) |
| `/hubs/dashboard` | SignalR |

First run: `GET /api/v1/setup/needs-admin`, then `POST /api/v1/setup/create-admin` if needed.

```bash
dotnet test tests/OxyKit.Mail.Tests/OxyKit.Mail.Tests.csproj
dotnet test tests/OxyKit.Mail.Client.Tests/OxyKit.Mail.Client.Tests.csproj
```

**Smoke:** import [`hoppscotch/`](./hoppscotch/) collection + env → **OxyKit.Mail Local** → run folders in order.

**SMTP OAuth2** (M365 / Google): see [ENVIRONMENT_SETTINGS.md](./ENVIRONMENT_SETTINGS.md#smtp-oauth2).

## NuGet (Client)

```bash
dotnet add package OxyKit.Mail.Client
```

Also: `OxyKit.Mail.Contracts` (transitive). `MailClient.SendAsync` only — not a dashboard SDK. Auth: API key or Bearer. Tag `v*` → [docs/NUGET_PUBLISH.md](./docs/NUGET_PUBLISH.md).

**.NET Framework 2.0 / 4.5+:** do not use the Client package — call HTTP per the [Framework HOW-TO](https://oxykit.dev/integrate/framework).

Local suite checkout (optional):

```xml
<ProjectReference Include="..\OxyKit.Mail\src\OxyKit.Mail.Client\OxyKit.Mail.Client.csproj" />
```

## Docs

| Doc | Role |
|-----|------|
| [AGENTS.md](./AGENTS.md) | Layout, auth, ErrorOr, change checklist |
| [ENVIRONMENT_SETTINGS.md](./ENVIRONMENT_SETTINGS.md) | `.env` / config / DKIM / OAuth2 |
| [oxykit.dev/mail](https://oxykit.dev/mail/) | Product docs |
| [hoppscotch/](./hoppscotch/) | Manual HTTP smoke |
| [CHANGELOG.md](./CHANGELOG.md) | Keep a Changelog |
| `/scalar` · `/openapi/v1.json` | Live API shapes (Development) |
| [LICENSE](./LICENSE) | MIT |
