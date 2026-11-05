# Environment settings

Config: `appsettings*.json` + optional solution-root **`.env`** (DotNetEnv in Development).  
`__` → nested keys (`Jwt__Key` → `Jwt:Key`). Secrets stay in env / secrets manager — never commit real `.env`.

## Minimal `.env` (local SQLite)

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

## Database

| Setting | Notes |
|---------|-------|
| `DatabaseProvider` | `SqlServer`, `PostgreSQL`, or `SQLite` |
| `ConnectionStrings__SqlServerConnection` | |
| `ConnectionStrings__PostgreSQLConnection` | |
| `ConnectionStrings__SQLiteConnection` | e.g. `Data Source=OxyKit.Mail.db` |

## JWT (`Jwt`)

| Setting | Notes |
|---------|-------|
| `Jwt__Key` | **Required**, 32+ chars |
| `Jwt__Issuer` / `Jwt__Audience` | |
| `Jwt__ExpiresHours` | Default `8` |

## SMTP (`SmtpSettings`)

| Setting | Notes |
|---------|-------|
| `Host` / `Port` / `Username` / `EnableSsl` | |
| `Password` | Required when `AuthMode=Password` |
| `AuthMode` | `Password` (default) or `OAuth2` |
| `OAuth2__Provider` | `Microsoft` or `Google` |
| `OAuth2__GrantType` | `RefreshToken` (default) or `ClientCredentials` (Microsoft only) |
| `OAuth2__ClientId` / `ClientSecret` / `RefreshToken` | |
| `OAuth2__TenantId` | Required for Microsoft |
| `OAuth2__Scope` / `Authority` / `TokenEndpoint` | Optional; defaults below |

### SMTP OAuth2

Prefer a one-time **refresh token** (interactive consent), then store it in env. Password auth remains the default.

**Defaults**

| Provider + grant | Scope |
|------------------|-------|
| Microsoft + RefreshToken | `https://outlook.office365.com/SMTP.Send offline_access` |
| Microsoft + ClientCredentials | `https://outlook.office365.com/.default` |
| Google + RefreshToken | `https://mail.google.com/` |

**Microsoft 365 (refresh token) `.env`**

```env
SmtpSettings__Host=smtp.office365.com
SmtpSettings__Port=587
SmtpSettings__EnableSsl=true
SmtpSettings__Username=sender@contoso.com
SmtpSettings__AuthMode=OAuth2
SmtpSettings__OAuth2__Provider=Microsoft
SmtpSettings__OAuth2__GrantType=RefreshToken
SmtpSettings__OAuth2__TenantId=...
SmtpSettings__OAuth2__ClientId=...
SmtpSettings__OAuth2__ClientSecret=...
SmtpSettings__OAuth2__RefreshToken=...
```

**Google (refresh token) `.env`**

```env
SmtpSettings__Host=smtp.gmail.com
SmtpSettings__Port=587
SmtpSettings__EnableSsl=true
SmtpSettings__Username=sender@gmail.com
SmtpSettings__AuthMode=OAuth2
SmtpSettings__OAuth2__Provider=Google
SmtpSettings__OAuth2__GrantType=RefreshToken
SmtpSettings__OAuth2__ClientId=...
SmtpSettings__OAuth2__ClientSecret=...
SmtpSettings__OAuth2__RefreshToken=...
```

Obtain refresh tokens via Entra / Google Cloud consent (mailbox must allow SMTP AUTH). Microsoft also supports `GrantType=ClientCredentials` for app-only Exchange Online.

## DKIM (`DkimSettings`)

Off by default. Enable only with a real domain + private key on disk.

| Setting | Default | Notes |
|---------|---------|-------|
| `Enabled` | `false` | |
| `Domain` / `PrivateKeyPath` | | Required when enabled |
| `Selector` | `default` | |
| `HeadersToSign` | from,to,subject,date,message-id | |
| Canonicalization / `SignatureAlgorithm` | `relaxed` / `rsa-sha256` | |

Admin helpers: `POST /api/v1/email/dkim/generate-keys`, `GET /api/v1/email/dkim/dns-record`. Publish TXT `<selector>._domainkey.<domain>`.

## Queue, API keys, limits

| Area | Keys (defaults in appsettings) |
|------|--------------------------------|
| Queue | `QueueSettings__PollIntervalSeconds` (10), `BatchSize` (10) |
| Sender | `EmailSenderSettings__MaxRetries` (3), `DelayMs` (2000) |
| API keys | `ApiKeySettings__MaxAgeInDays` (90), `WarnBeforeExpiryDays` (14), `MaxInactiveTimeInDays` (30) |
| Rate limit | `RateLimit__{Auth,Email,Api}__{PermitLimit,WindowSeconds,QueueLimit}` |
| Request size | `RequestLimits__MaxRequestBodySize` (10 MB), `MaxEmailBodySize` / `MaxAttachmentSize` (5 MB) |
| CORS | `Cors__AllowedOrigins__0` (e.g. `http://localhost:5173`), `Cors__AllowCredentials` |

## Mail templates (`MailTemplates`)

| Setting | Default | Notes |
|---------|---------|-------|
| `MailTemplates__TemplatesRoot` | `templates` | Relative to content root, or absolute. Pack is `subject.txt` + `body.html` |
| `MailTemplates__MaxSubjectChars` | `10000` | |
| `MailTemplates__MaxBodyChars` | `500000` | |
| `MailTemplates__MaxTemplatePackBytes` | `2097152` | Zip upload cap (2 MiB) |

Compose mounts this root at `/data/templates` and seeds it when empty. Send uses `templateId` + JSON object `data`.

## Observability

Implemented via NuGet packages `OxyKit.Observability` + `OxyKit.OpenTelemetry` (see [oxykit.dev/guide/observability](https://oxykit.dev/guide/observability)).

| Setting | Notes |
|---------|-------|
| `Logging__JsonConsole` | Force JSON console on (`true`) or off (`false`); default JSON outside Development |
| `OpenTelemetry__Enabled` | Opt-in OTLP traces + metrics (default `false`) |
| `OpenTelemetry__ServiceName` | Resource service name (default `OxyKit.Mail`) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Also enables OTel when set (standard OTel env) |
| `OTEL_SERVICE_NAME` | Overrides service name when set |

Inbound/outbound correlation header: `X-Correlation-Id`.

## Where defaults live

- `src/OxyKit.Mail/appsettings.json` — shared  
- `appsettings.Development.json` / `Production.json` — env overlays
- Solution-root `.env` — secrets (gitignored)
