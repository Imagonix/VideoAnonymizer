param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$PythonExe = "",
    [switch]$SkipPythonBuild,
    [switch]$SkipVueBuild
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$artifacts = Join-Path $root "artifacts"
$publishRoot = Join-Path $artifacts "standalone"
$publishDir = Join-Path $publishRoot "bin"
$objectDetectionProject = Join-Path $root "VideoAnonymizer.ObjectDetection"
$objectDetectionDist = Join-Path $artifacts "object-detection"
$objectDetectionSpec = Join-Path $objectDetectionProject "VideoAnonymizer.ObjectDetection.spec"
$videoEditorProject = Join-Path $root "VideoAnonymizer.Web.Modules\ClientApp\video-editor"
$standaloneProject = Join-Path $root "VideoAnonymizer.StandaloneHost\VideoAnonymizer.StandaloneHost.csproj"
$launcherProject = Join-Path $root "VideoAnonymizer.StandaloneLauncher\VideoAnonymizer.StandaloneLauncher.csproj"

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

if (-not $SkipPythonBuild) {
    if ([string]::IsNullOrWhiteSpace($PythonExe)) {
        foreach ($candidate in @("python", "py")) {
            try {
                & $candidate --version *> $null
                if ($LASTEXITCODE -eq 0) {
                    $PythonExe = $candidate
                    break
                }
            }
            catch {
                $global:LASTEXITCODE = 1
            }
        }
    }
    else {
        try {
            & $PythonExe --version *> $null
        }
        catch {
            $global:LASTEXITCODE = 1
        }
    }

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($PythonExe)) {
        throw "No usable Python executable was found. Install Python, pass -PythonExe py, pass -PythonExe C:\Path\python.exe, or use -SkipPythonBuild when the ObjectDetection exe was already built."
    }

    Push-Location $objectDetectionProject
    try {
        & $PythonExe -m PyInstaller $objectDetectionSpec `
            --distpath $objectDetectionDist `
            --workpath (Join-Path $artifacts "pyinstaller-work") `
            --clean `
            -y

        if ($LASTEXITCODE -ne 0) {
            throw "PyInstaller failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

if (Test-Path $publishRoot) {
    try {
        Remove-Item $publishRoot -Recurse -Force
    }
    catch {
        throw "Could not clean '$publishRoot'. Stop the running standalone app/ObjectDetection process and rerun the publish script. Original error: $($_.Exception.Message)"
    }
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

if (-not $SkipVueBuild) {
    $npmCommand = Get-Command "npm.cmd" -ErrorAction SilentlyContinue
    if (-not $npmCommand) {
        $npmCommand = Get-Command "npm" -ErrorAction SilentlyContinue
    }

    if (-not $npmCommand) {
        throw "npm was not found. Install Node.js/npm or rerun with -SkipVueBuild when the Vue components are already built."
    }

    Push-Location $videoEditorProject
    try {
        if (-not (Test-Path (Join-Path $videoEditorProject "node_modules"))) {
            if (Test-Path (Join-Path $videoEditorProject "package-lock.json")) {
                & $npmCommand.Source ci
            }
            else {
                & $npmCommand.Source install
            }

            if ($LASTEXITCODE -ne 0) {
                throw "npm dependency restore failed with exit code $LASTEXITCODE."
            }
        }

        & $npmCommand.Source run build

        if ($LASTEXITCODE -ne 0) {
            throw "Vue component build failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

dotnet publish $standaloneProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

foreach ($serviceExe in @(
    "VideoAnonymizer.ApiService.exe",
    "VideoAnonymizer.VideoProcessor.exe"
)) {
    $serviceExePath = Join-Path $publishDir $serviceExe
    if (Test-Path $serviceExePath) {
        Remove-Item $serviceExePath -Force
    }
}

$indexHtml = Join-Path $publishDir "wwwroot\index.html"
if (Test-Path $indexHtml) {
    $frameworkDir = Join-Path $publishDir "wwwroot\_framework"
    $blazorWebAssemblyScript = Get-ChildItem (Join-Path $publishDir "wwwroot\_framework") `
        -Filter "blazor.webassembly*.js" `
        -File |
        Where-Object { $_.Extension -eq ".js" } |
        Select-Object -First 1

    if ($blazorWebAssemblyScript) {
        $html = Get-Content $indexHtml -Raw
        $html = $html.Replace(
            "_framework/blazor.webassembly#[.{fingerprint}].js",
            "_framework/$($blazorWebAssemblyScript.Name)")
        Set-Content -Path $indexHtml -Value $html -NoNewline
    }
    else {
        Write-Warning "Could not find blazor.webassembly*.js in the published _framework directory."
    }

    foreach ($alias in @(
        @{
            Pattern = "dotnet.*.js"
            Name = "dotnet.js"
            Exclude = @("dotnet.native.*", "dotnet.runtime.*")
        },
        @{
            Pattern = "dotnet.native.*.js"
            Name = "dotnet.native.js"
            Exclude = @()
        },
        @{
            Pattern = "dotnet.runtime.*.js"
            Name = "dotnet.runtime.js"
            Exclude = @()
        }
    )) {
        $script = Get-ChildItem $frameworkDir -Filter $alias.Pattern -File |
            Where-Object {
                $fileName = $_.Name
                -not ($alias.Exclude | Where-Object { $fileName -like $_ })
            } |
            Select-Object -First 1

        if ($script) {
            Copy-Item $script.FullName (Join-Path $frameworkDir $alias.Name) -Force
        }
        else {
            Write-Warning "Could not find $($alias.Pattern) in the published _framework directory."
        }
    }
}

$objectDetectionOutput = Join-Path $objectDetectionDist "VideoAnonymizer.ObjectDetection"
$targetObjectDetection = Join-Path $publishDir "ObjectDetection"

if (Test-Path $objectDetectionOutput) {
    if (Test-Path $targetObjectDetection) {
        try {
            Remove-Item $targetObjectDetection -Recurse -Force
        }
        catch {
            throw "Could not replace '$targetObjectDetection'. Stop the running standalone app/ObjectDetection process and rerun the publish script. Original error: $($_.Exception.Message)"
        }
    }

    Copy-Item $objectDetectionOutput $targetObjectDetection -Recurse
}
else {
    Write-Warning "ObjectDetection output not found at $objectDetectionOutput. Use -SkipPythonBuild only when it was copied manually."
}

$targetModelDir = Join-Path $publishDir "data\models"
New-Item -ItemType Directory -Force -Path $targetModelDir | Out-Null

$sourceModelCandidates = @(
    (Join-Path $root "data\models\FaceDetector.onnx"),
    (Join-Path $objectDetectionProject "models\FaceDetector.onnx")
)

$sourceModel = $sourceModelCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($sourceModel) {
    Copy-Item $sourceModel (Join-Path $targetModelDir "FaceDetector.onnx") -Force
}
else {
    Write-Warning "FaceDetector.onnx was not found locally. The standalone app will download it on first start when network access is available."
}

dotnet publish $launcherProject `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $publishRoot

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish for launcher failed with exit code $LASTEXITCODE."
}

Get-ChildItem $publishRoot -File |
    Where-Object { $_.Name -ne "VideoAnonymizer.exe" } |
    Remove-Item -Force

Write-Host "Standalone package published to $publishRoot"
Write-Host "Start with: $publishRoot\VideoAnonymizer.exe"
