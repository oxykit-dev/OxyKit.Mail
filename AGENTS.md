# OxyKit.Mail — agent guide

Token-efficient map. Prefer this over README / OpenAPI dumps.

## What it is

**OxyKit** suite core. .NET 10 email delivery: queue + retries, DKIM, JWT + API keys, dashboard, SignalR. Console: sibling **`../oxykit-console/`**.

**MIT** · host version on `OxyKit.Mail`; Client/Contracts packable (nuget.org via Trusted Publishing / `release-nuget.yml` when their `<Version>` matches the tag). See `docs/NUGET_PUBLISH.md`.

**vs Flow:** Mail owns SMTP (send / queue / retry / dead-letter). `POST /email/send` → `sent` (200) | `accepted`/`scheduled` (202). Optional `Idempotency-Key` returns the existing acceptance. Callers **must not re-send** on `accepted`. Flow only orchestrates jobs.

## Open these first

| Task | Read |
|------|------|
| Startup / DI | `src/OxyKit.Mail/Program.cs`, `Common/Extensions/ServiceCollectionExtensions.cs` |
| ErrorOr → HTTP | `src/OxyKit.Mail/Common/MailEndpoint.cs` |
| Auth schemes | `src/OxyKit.Mail/Common/AuthSchemes.cs` |
| Pipeline | `src/OxyKit.Mail/Common/Extensions/ApplicationBuilderExtensions.cs` |
| Identity / users | `src/OxyKit.Mail/Features/Identity/` |
| API keys | `src/OxyKit.Mail/Features/ApiKeys/` |
| Send / queue / DKIM / SMTP | `src/OxyKit.Mail/Features/Email/` |
| Mail template packs | `src/OxyKit.Mail/Features/Templates/` (`subject.txt` + `body.html`; send via `templateId` + `data`) |
| Dashboard / SignalR | `src/OxyKit.Mail/Features/Dashboard/` |
| Roles / EF | `src/OxyKit.Mail.Infrastructure/AppRoles.cs`, `ApplicationDbContext.cs`, `Entities/` |
| Migrations | `scripts/AddEfMigration.*.ps1`, provider `Migrations/` |
| Tests | `tests/OxyKit.Mail.Tests/` |
| Hoppscotch | `hoppscotch/` (do not paste JSON into chat) |
| Config | `ENVIRONMENT_SETTINGS.md` |

Ignore `bin/`, `obj/`, `output/`, `TestResults/`, `artifacts/`.

## Layout (canonical Pure VSA)

Suite reference host **code style**: flat features, ErrorOr in services, FastEndpoints, `MailEndpoint` + `SendOnErrorAsync`. Repo layout matches suite `src/` + `tests/`.

```text
OxyKit.Mail.slnx
src/
  OxyKit.Mail/                         # host (FastEndpoints)
  OxyKit.Mail.Contracts/               # send DTOs — packable
  OxyKit.Mail.Client/                  # send HttpClient SDK — packable; not dashboard
  OxyKit.Mail.Infrastructure/
  OxyKit.Mail.Infrastructure.{SqlServer,Postgres,Sqlite}/
tests/
  OxyKit.Mail.Tests/
  OxyKit.Mail.Client.Tests/
hoppscotch/
```

**Client surface:** `MailClient.SendAsync` only (+ options / exception). Integrators use API key (or Bearer). Do not grow Client into Identity/Dashboard/DKIM.  
**Framework 2.0 / 4.5+:** no Client — [`../docs/DOTNET_FRAMEWORK_INTEGRATION.md`](../docs/DOTNET_FRAMEWORK_INTEGRATION.md).

Auth = FastEndpoints `Roles(...)` / schemes only — no DB permission table. `Message` stays in Infrastructure (avoids Host↔Infra cycle).

### ErrorOr → HTTP

- Services: `ErrorOr<T>` when failure is possible; plain `Task<T>` when always succeeds.
- Endpoints: inherit `MailEndpoint*` → `if (await SendOnErrorAsync(result, ct)) return;`
- Map: NotFound→404, Forbidden→403, Unauthorized→401, Validation/Conflict/other→FE errors body.
- Unhandled: `ExceptionHandlingMiddleware` → `{ statusCode, message, errors }`.

## Observability

Shared libs (NuGet): `OxyKit.Observability` + `OxyKit.OpenTelemetry` — see [Observability AGENTS](https://github.com/oxykit-dev/OxyKit.Observability/blob/master/AGENTS.md).

- **Logs / correlation:** `AddOxyKitJsonConsole` + `UseOxyKitCorrelation`
- **OTel:** `AddOxyKitOpenTelemetry` with `MailTelemetry` source/meter names; off by default
- Product meters stay in `Common/Observability/MailTelemetry.cs`

## Auth (do not weaken)

- `X-Api-Key` → ApiKey scheme; else JWT Bearer
- Roles: `Admin`, `User`, `Sender` (`AppRoles.*`)
- Send: `Admin`|`Sender` + Bearer/ApiKey (`User` alone cannot send)
- Mail template packs: write `Admin`; list/get/pack `Admin`|`Sender`. Send with `templateId` + `data` renders `subject.txt` / `body.html` (body strings HTML-encoded; subject must stay one line)
- **Sender = send-only** — `POST /email/send`, Me, and own password. No dashboard stats, no dashboard SignalR hub, no identity/users/DKIM admin. Dashboard REST + hub: `Admin`|`User` only. The Mail UI hides those screens; it does not 403-wall them.
- Own password: `POST /auth/change-password` (v1), Bearer only, Admin|User|Sender. A wrong current password is validation **400**, never 401.
- JWT Bearer `OnMessageReceived`: accept `access_token` query for paths under `/hubs` (SignalR WebSockets)
- Bootstrap: anonymous `/setup/*` only when no admin exists
- Secrets in `.env` / secrets manager — never commit real credentials
- Strong JWT key (32+); keep rate limits and request size limits on

## Add an endpoint

1. Flat files under `src/OxyKit.Mail/Features/{Feature}/` (subfolders only `Services/` / `Configuration/`)
2. Inherit `MailEndpoint*` (not raw `Endpoint`)
3. `Configure()`: route, `Version(1)`, roles, schemes, Summary
4. `SendOnErrorAsync` for failures; DTOs only — never EF entities
5. Tests under `tests/OxyKit.Mail.Tests/Features/{Feature}/` (xUnit v3 / MTP; `dotnet test --project …`)
6. Public route → Hoppscotch request; config knob → `ENVIRONMENT_SETTINGS.md` + appsettings
7. Note in `CHANGELOG.md` `[Unreleased]` when behavior changes
8. Send contract change → update `OxyKit.Mail.Contracts` + `MailClient` tests; host validators stay in Features/Email

Do not change HTTP paths without coordinating `oxykit-console`. **API shapes:** Scalar (`/scalar`) + `/openapi/v1.json` (Development; suite convention). Testing also serves `/openapi/v1.json` (no Scalar) for contract asserts / snapshot export.

To export an OpenAPI snapshot after contract changes:

```bash
OXYKIT_EXPORT_OPENAPI="$(pwd)/oxykit-mail-api.json" \
  dotnet test --project tests/OxyKit.Mail.Tests/OxyKit.Mail.Tests.csproj --filter-class "*OpenApiDocumentTests"
```

## Commands

```bash
cd OxyKit.Mail
dotnet build OxyKit.Mail.slnx
dotnet run --project src/OxyKit.Mail/OxyKit.Mail.csproj   # :5026  Scalar /scalar
dotnet test --project tests/OxyKit.Mail.Tests/OxyKit.Mail.Tests.csproj
dotnet test --project tests/OxyKit.Mail.Client.Tests/OxyKit.Mail.Client.Tests.csproj
```

Console: `../oxykit-console` (`npm run dev`; proxies `/api/v1` to `:5026`).  
Hoppscotch: import `hoppscotch/*`, env **OxyKit.Mail Local**, run folders in order.

Suite map: `../AGENTS.md`.

## Docker image

`Dockerfile` (context = repo root) + `.github/workflows/docker.yml` publish `ghcr.io/oxykit-dev/oxykit-mail` for linux/amd64 + arm64: `v*` tag → `X.Y.Z`, `X.Y`, `latest`; `master` → `edge`; every build → `sha-<short>`. PRs build without pushing. The Compose stack + smoke test live in [oxykit-compose](https://github.com/oxykit-dev/oxykit-compose).
