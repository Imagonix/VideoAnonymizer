### Add Migration

Run from this directory via the script: `.\add-migrations.ps1 -Name "<Name>"`

Or creating the migrations one by one:

```bash
# Postgres
dotnet ef migrations add <Name> --project ../VideoAnonymizer.Database.Postgres/ --startup-project ../VideoAnonymizer.Database.MigrationService/ --output-dir Migrations
# SQLite
dotnet ef migrations add <Name> --project ../VideoAnonymizer.Database.SQLite/ --output-dir Migrations
```