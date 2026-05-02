### Add Migration

Run from this directory:

```bash
# Postgres
dotnet ef migrations add <Name> --project ../VideoAnonymizer.Database.Postgres/ --startup-project ../VideoAnonymizer.Database.MigrationService/ --output-dir Migrations
# SQLite
dotnet ef migrations add <Name> --project ../VideoAnonymizer.Database.SQLite/ --output-dir Migrations
```

Or from repo root via the script: `.\add-migrations.ps1 -Name "<Name>"`
