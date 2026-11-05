# Changelog

Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

### Added

- `POST /api/v1/auth/change-password` — signed-in Admin, User, or Sender replaces their own password (current password required; Bearer only)
- Mail template packs: `GET/PUT/DELETE /api/v1/templates/{id}`, `GET .../pack`; seed `templates/welcome/`
- `SendEmailRequest.TemplateId` + `Data` — Scriban render of `subject.txt` / `body.html` (HTML-escape body strings; rendered subject must be one line); Admin pack CRUD, Admin|Sender list/get. Zip entries are capped while extracting
- Compose: `MailTemplates__TemplatesRoot=/data/templates` on `mail-data` volume + seed entrypoint

## [0.1.6] - 2026-11-04

### Added

- Observability via NuGet `OxyKit.Observability` + `OxyKit.OpenTelemetry` (JSON console, `X-Correlation-Id`, optional OTLP); `MailTelemetry` meters/Activity for queue send
- Testing host serves `/openapi/v1.json`; `OpenApiDocumentTests` asserts core routes (incl. DKIM) and can export the mail-web snapshot via `OXYKIT_EXPORT_OPENAPI`

### Changed

- **Sender = send-only:** document policy; `DashboardHub` requires `Admin`|`User` (Sender cannot join). JWT Bearer reads `access_token` query for SignalR `/hubs`. Dashboard REST endpoints already excluded Sender
- Release workflow: `v*` always ships host zips; nuget.org only when Client/Contracts `<Version>` matches the tag (host `<Version>` on `OxyKit.Mail`)

## [0.1.5] - 2026-10-27

### Added

- Optional `Idempotency-Key` header on `POST /api/v1/email/send` — same key returns the existing acceptance (`emailId` / status) without creating or re-sending
- `MailClient.SendAsync(request, idempotencyKey, ct)` overload; Contracts body unchanged

### Changed

- `Message.IdempotencyKey` + unique index (filtered on SQL Server); migrations for Sqlite / Postgres / SqlServer
- Idempotency insert race handling only on unique-constraint violations (`DbUpdateExceptionExtensions`)
- SQL Server idempotency unique index uses filtered `IS NOT NULL` in the EF model (matches migration)
- Idempotent replay: `scheduled` only while `ScheduledFor` is still in the future; due/queued → `accepted`

## [0.1.0] - 2026-10-22

### Added

- `OxyKit.Mail.Contracts` + `OxyKit.Mail.Client` (send-focused SDK; `net10.0`)
- Suite HOW-TO: .NET Framework 2.0 (`HttpWebRequest`) / 4.5+ (`HttpClient`) Mail send ([`docs/DOTNET_FRAMEWORK_INTEGRATION.md`](../docs/DOTNET_FRAMEWORK_INTEGRATION.md))
- SMTP OAuth2 (Microsoft / Google, config-only)
- Hoppscotch smoke (`hoppscotch/`) + `/health` integration smoke
- ApiKeyService unit tests; `IApiKeyRepository`
- Trusted Publishing release workflow (`.github/workflows/release-nuget.yml`); package READMEs + symbol packages

### Changed

- Send DTOs live in Contracts (host + Client share wire types)
- OpenAPI: document `v1` at `/openapi/v1.json`, `ShortSchemaNames` (suite convention with Scalar)
- Test projects: MSTest → **xUnit** → **xunit.v3.mtp-v2** (`OutputType=Exe`, `global.json` → MTP); Client tests gain **FluentAssertions** (suite pin)
- Repo layout: production under `src/`, tests under `tests/` (suite-aligned `*.slnx`)
- Send accept contract: `POST /api/v1/email/send` returns `{ emailId, status, message }` with **200 `sent`** or **202 `accepted`|`scheduled`** (orchestrators must not re-send on `accepted`)
- Host pattern: `MailEndpoint*` + `SendOnErrorAsync`; FE-shaped unhandled errors; flat role endpoints; ApiKey types renamed (`CreateApiKey*`, …)
- Dashboard / list-users / list-api-keys use plain `Task` (no fake `ErrorOr`); empty user list is `[]`
- DKIM: conditional options validator; SMTP health probe 3s timeout; Testing host uses InMemory + skips background services
