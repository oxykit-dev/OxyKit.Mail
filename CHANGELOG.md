# Changelog

Format: [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [Unreleased]

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
