# Copilot — OxyKit.Mail

Follow **[AGENTS.md](../AGENTS.md)**.

- Pure VSA: `src/OxyKit.Mail/Features/{Identity,ApiKeys,Email,Templates,Dashboard}/` (flat; only `Services/` / `Configuration/` subfolders)
- Endpoints: `MailEndpoint*` + `SendOnErrorAsync`; persistence in `src/OxyKit.Mail.Infrastructure` — no Domain project
- Auth: FastEndpoints `Roles(...)` + JWT / `X-Api-Key` — no DB permission table
- Tests: xUnit under `tests/OxyKit.Mail.Tests/`; public routes → `hoppscotch/`
- HTTP shapes: Scalar (`/scalar`), not a hand-maintained endpoints doc
- Do not change public paths without coordinating `../oxykit-console`
- Changelog: Keep a Changelog; cut a section on each tag
