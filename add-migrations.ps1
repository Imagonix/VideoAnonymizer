param(
    [Parameter(Mandatory = $true)]
    [string]$Name
)

$ErrorActionPreference = "Stop"
$dbDir = Join-Path $PSScriptRoot "VideoAnonymizer.Database"

# Postgres — uses MigrationService as startup project to resolve Npgsql connection
Push-Location $dbDir
try {
    dotnet ef migrations add $Name `
        --project ../VideoAnonymizer.Database.Postgres/ `
        --startup-project ../VideoAnonymizer.Database.MigrationService/ `
        --output-dir Migrations
} finally { Pop-Location }

# SQLite — has its own IDesignTimeDbContextFactory
Push-Location $dbDir
try {
    dotnet ef migrations add $Name `
        --project ../VideoAnonymizer.Database.SQLite/ `
        --output-dir Migrations
} finally { Pop-Location }
