# run-tests.ps1

$projectPath = "..\VideoAnonymizer.Web.Modules.Tests.Harness\VideoAnonymizer.Web.Modules.Tests.Harness\VideoAnonymizer.Web.Modules.Tests.Harness.csproj"
$url = "http://localhost:5217"

Write-Host "Starting harness..."
$process = Start-Process "dotnet" -ArgumentList "run --project `"$projectPath`"" -PassThru

Write-Host "Waiting for app to be ready..."
for ($i = 0; $i -lt 30; $i++) {
    try {
        Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 1 | Out-Null
        Write-Host "App is ready!"
        break
    } catch {
        Start-Sleep -Seconds 1
    }
}

Write-Host "Running Playwright tests..."
npx playwright-bdd test
npx playwright test --workers=8

Write-Host "Stopping harness..."
Stop-Process -Id $process.Id -Force