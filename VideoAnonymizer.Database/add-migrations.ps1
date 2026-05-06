param(
    [Parameter(Mandatory = $true)]
    [string]$Name
)

$ErrorActionPreference = "Stop"

dotnet ef migrations add $Name `
	--project ../VideoAnonymizer.Database.Postgres/ `
	--startup-project ../VideoAnonymizer.Database.MigrationService/ `
	--output-dir Migrations
		
dotnet ef migrations add $Name `
	--project ../VideoAnonymizer.Database.SQLite/ `
	--startup-project ../VideoAnonymizer.Database.SQLite/ `
	--output-dir Migrations
