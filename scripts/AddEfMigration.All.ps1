param(
    [string]$MigrationName
)

if (-not $MigrationName) {
    if ($args.Count -gt 0) {
        $MigrationName = $args[0]
    }
}

if (-not $MigrationName) {
    throw "MigrationName parameter is required. Usage: .\AddEfMigration.All.ps1 MigrationName"
}

$envPath = "./.env"
$providers = @(
  @{ Name = "PostgreSQL"; Project = "src/OxyKit.Mail.Infrastructure.Postgres" },
    @{ Name = "SQLite"; Project = "src/OxyKit.Mail.Infrastructure.Sqlite" },
    @{ Name = "SqlServer"; Project = "src/OxyKit.Mail.Infrastructure.SqlServer" }
)

$startupProject = "src/OxyKit.Mail"
$context = "ApplicationDbContext"

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "Creating migrations for all database providers" -ForegroundColor Cyan
Write-Host "==================================================`n" -ForegroundColor Cyan

foreach ($provider in $providers) {
    Write-Host "Processing: $($provider.Name)" -ForegroundColor Yellow
    Write-Host "Project: $($provider.Project)" -ForegroundColor Gray
    
    $lines = Get-Content $envPath

    $newLines = @()
    foreach ($line in $lines) {
        if ($line -match '^\s*#?\s*DatabaseProvider=') {
        if ($line -match "DatabaseProvider=$($provider.Name)\b") {
        # Uncomment the line for the selected provider
 $newLines += "DatabaseProvider=$($provider.Name)"
         } else {
       # Comment out all other DatabaseProvider lines
       $lineStripped = $line -replace '^\s*#\s*', ''
   $newLines += "#$lineStripped"
            }
      } else {
        $newLines += $line
        }
    }

    Set-Content $envPath $newLines

    Write-Host "  ? Set DatabaseProvider=$($provider.Name) in .env" -ForegroundColor Green

    # The context is in OxyKit.Mail.Infrastructure, but migrations go to provider-specific project
    dotnet ef migrations add "$MigrationName" `
        --project $provider.Project `
  --startup-project $startupProject `
        --context $context `
     --output-dir Migrations

    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ? Migration created successfully for $($provider.Name)`n" -ForegroundColor Green
    } else {
    Write-Host "  ? Failed to create migration for $($provider.Name)`n" -ForegroundColor Red
    }
}

Write-Host "==================================================`n" -ForegroundColor Cyan
Write-Host "Migration creation complete!" -ForegroundColor Cyan
Write-Host "Check the output above for any errors.`n" -ForegroundColor Gray