# OxyKit.Mail — agent guide

Token-efficient map. Prefer this over README / OpenAPI dumps.

## What it is

**OxyKit** suite core. .NET 10 email delivery: queue + retries, DKIM, JWT + API keys, dashboard, SignalR. UI: sibling **`../oxykit-mail-web/`**.

**MIT** · v0.1 — Client/Contracts packable (nuget.org via Trusted Publishing / `release-nuget.yml`). See `docs/NUGET_PUBLISH.md`.

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
| Dashboard / SignalR | `src/OxyKit.Mail/Features/Dashboard/` |
| Roles / EF | `src/OxyKit.Mail.Infrastructure/AppRoles.cs`, `ApplicationDbContext.cs`, `Entities/` |
| Migrations | `scripts/AddEfMigration.*.ps1`, provider `Migrations/` |
| Tests | `tests/OxyKit.Mail.Tests/` |
| Hoppscotch | `hoppscotch/` (do not paste JSON into chat) |
| Config | `ENVIRONMENT_SETTINGS.md` |

Ignore `bin/`, `obj/`, `TestResults/`.

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

## Auth (do not weaken)

- `X-Api-Key` → ApiKey scheme; else JWT Bearer
- Roles: `Admin`, `User`, `Sender` (`AppRoles.*`)
- Send: `Admin`|`Sender` + Bearer/ApiKey
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

Do not change HTTP paths without coordinating `oxykit-mail-web`. **API shapes:** Scalar (`/scalar`) + `/openapi/v1.json` (Development; suite convention). After contract changes, regenerate `../oxykit-mail-web/oxykit-mail-api.json` from that JSON.


## Commands

```bash
cd OxyKit.Mail
dotnet build OxyKit.Mail.slnx
dotnet run --project src/OxyKit.Mail/OxyKit.Mail.csproj   # :5026  Scalar /scalar
dotnet test tests/OxyKit.Mail.Tests/OxyKit.Mail.Tests.csproj
dotnet test tests/OxyKit.Mail.Client.Tests/OxyKit.Mail.Client.Tests.csproj
```

Frontend: `../oxykit-mail-web` with `VITE_BACKEND_URL=http://localhost:5026`.  
Hoppscotch: import `hoppscotch/*`, env **OxyKit.Mail Local**, run folders in order.

Suite map: `../AGENTS.md`.
