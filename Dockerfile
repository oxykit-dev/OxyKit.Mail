# syntax=docker/dockerfile:1
# Image: ghcr.io/oxykit-dev/oxykit-mail (published by .github/workflows/docker.yml).
# Framework-dependent portable publish, so the build stage runs natively for every target platform.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Build.targets ./
COPY src/ src/
RUN dotnet publish src/OxyKit.Mail/OxyKit.Mail.csproj -c Release -o /app/publish \
    && rm -f /app/publish/appsettings.Development.json /app/publish/.env /app/publish/*.db

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY docker/entrypoint.sh /app/entrypoint.sh
ENV ASPNETCORE_URLS=http://+:5026
ENV MailTemplates__TemplatesRoot=/data/templates
ENV OXYKIT_MAIL_TEMPLATES_SEED=/app/templates
EXPOSE 5026
# /health is 503 while SMTP is unreachable; any HTTP response means the process is up.
HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=6 \
    CMD curl -s -o /dev/null http://localhost:5026/health || exit 1
ENTRYPOINT ["/app/entrypoint.sh"]
