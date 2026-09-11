param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$PythonExe = "",
    [switch]$SkipPythonBuild,
    [switch]$SkipVueBuild,
    [switch]$BundleCudaDependencies
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

function Remove-BundledCudaDependencies {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ($BundleCudaDependencies -or -not (Test-Path $Path)) {
        return
    }

    $cudaDependencyPatterns = @(
        "cublas*.dll",
        "cudart*.dll",
        "cudnn*.dll",
        "cufft*.dll",
        "curand*.dll",
        "cusolver*.dll",
        "cusparse*.dll",
        "nccl*.dll",
        "npp*.dll",
        "nvjitlink*.dll",
        "nvrtc*.dll"
    )

    $filesToRemove = foreach ($pattern in $cudaDependencyPatterns) {
        Get-ChildItem -LiteralPath $Path -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue
    }

    $filesToRemove = $filesToRemove | Sort-Object -Property FullName -Unique

    $removedCount = 0
    [long]$removedBytes = 0

    foreach ($file in $filesToRemove) {
        $removedBytes += $file.Length
        $removedCount++
        Remove-Item -LiteralPath $file.FullName -Force
    }

    if ($removedCount -gt 0) {
        $removedMb = [math]::Round($removedBytes / 1MB, 2)
        Write-Host "Removed $removedCount bundled CUDA dependency file(s) from $Path ($removedMb MB). GPU mode will use CUDA/cuDNN from the system PATH."
    }
}

function Remove-StandalonePublishBloat {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    foreach ($relativePath in @(
        "App_Data",
        "BlazorDebugProxy"
    )) {
        $target = Join-Path $Path $relativePath
        if (Test-Path $target) {
            Remove-Item -LiteralPath $target -Recurse -Force
            Write-Host "Removed publish-only payload: $target"
        }
    }
}

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

        Remove-BundledCudaDependencies -Path (Join-Path $objectDetectionDist "VideoAnonymizer.ObjectDetection")
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

Remove-StandalonePublishBloat -Path $publishDir

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
    Remove-BundledCudaDependencies -Path $targetObjectDetection
}
else {
    Write-Warning "ObjectDetection output not found at $objectDetectionOutput. Use -SkipPythonBuild only when it was copied manually."
}

$targetModelDir = Join-Path $publishDir "data\models"
New-Item -ItemType Directory -Force -Path $targetModelDir | Out-Null

$modelSourceDirs = @(
    (Join-Path $objectDetectionProject "models"),
    (Join-Path $root "data\models")
) | Where-Object { Test-Path $_ }

$copiedModelCount = 0
foreach ($modelSourceDir in $modelSourceDirs) {
    Get-ChildItem $modelSourceDir -File |
        Where-Object { $_.Name -like "*.onnx" -or $_.Name -like "*.detector.json" } |
        ForEach-Object {
            Copy-Item $_.FullName (Join-Path $targetModelDir $_.Name) -Force
            if ($_.Extension -eq ".onnx") {
                $copiedModelCount++
            }
        }
}

if ($copiedModelCount -eq 0) {
    Write-Warning "No ONNX object detection models were found locally. The standalone AI service will not start until model files are present in data\models."
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
