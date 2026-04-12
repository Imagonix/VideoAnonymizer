$project = ".\VideoAnonymizer.AppHost"

Write-Host "Initializing user secrets..."

dotnet user-secrets init --project $project

function New-RandomPassword($length = 24) {
    $chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()"
    -join ((1..$length) | ForEach-Object { $chars | Get-Random })
}

$rabbitUser = "rabbituser"
$rabbitPassword = New-RandomPassword
$postgresPassword = New-RandomPassword

Write-Host "Setting development secrets..."

dotnet user-secrets set "Parameters:rabbit-user" $rabbitUser --project $project
dotnet user-secrets set "Parameters:rabbit-password" $rabbitPassword --project $project
dotnet user-secrets set "Parameters:postgres-password" $postgresPassword --project $project

Write-Host "Development secrets configured successfully!"