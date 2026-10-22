# OxyKit.Mail.Client

Typed HttpClient SDK for [OxyKit.Mail](https://oxykit.dev/mail/) send (`net10.0`). `MailClient.SendAsync` only — not a dashboard / identity / DKIM client.

```bash
dotnet add package OxyKit.Mail.Client
```

Pulls in `OxyKit.Mail.Contracts` transitively. Auth: API key (`X-Api-Key`) or Bearer.

**.NET Framework 2.0 / 4.5+:** use raw HTTP — see [Framework integration](https://oxykit.dev/integrate/framework).

Docs: [oxykit.dev](https://oxykit.dev) · Source: [oxykit-dev/OxyKit.Mail](https://github.com/oxykit-dev/OxyKit.Mail)
