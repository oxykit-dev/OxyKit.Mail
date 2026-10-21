param(
    [string]$MigrationName
)

if (-not $MigrationName) {
    if ($args.Count -gt 0) {
        $MigrationName = $args[0]
    }
}

if (-not $MigrationName) {
    throw "MigrationName parameter is required. Usage: .\AddEfMigration.Postgres.ps1 MigrationName"
}

$project = "src/OxyKit.Mail.Infrastructure.Postgres"
$startupProject = "src/OxyKit.Mail"
$context = "ApplicationDbContext"

# The context is in OxyKit.Mail.Infrastructure, but migrations go to provider-specific project
dotnet ef migrations add $MigrationName `
    --project $project `
    --startup-project $startupProject `
    --context $context `
    --output-dir Migrations

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migration '$MigrationName' created successfully in $project" -ForegroundColor Green
} else {
    Write-Host "Failed to create migration. Check the error above." -ForegroundColor Red
}