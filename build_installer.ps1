[CmdletBinding()]
param(
    [switch]$Standalone,
    [switch]$Both
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $ScriptDir

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "   Mawaqit Prayer Times - Installer Build Script" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# 1. Locate ISCC.exe (Inno Setup Compiler)
$isccCandidates = @(
    "C:\Program Files\Inno Setup 7\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 7\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

$isccPath = $null
foreach ($path in $isccCandidates) {
    if (Test-Path $path) {
        $isccPath = $path
        break
    }
}

if (-not $isccPath) {
    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) {
        $isccPath = $cmd.Source
    }
}

if (-not $isccPath) {
    Write-Error "Inno Setup Compiler (ISCC.exe) was not found. Please install Inno Setup 6 or 7."
    exit 1
}

Write-Host "[OK] Inno Setup Compiler found: $isccPath" -ForegroundColor Green

# Synchronize logo.ico from Desktop if available
$desktopLogo = "C:\Users\1\Desktop\logo.ico"
if (Test-Path $desktopLogo) {
    Copy-Item -Path $desktopLogo -Destination "$ScriptDir\Resources\logo.ico" -Force
    Copy-Item -Path $desktopLogo -Destination "$ScriptDir\logo.ico" -Force
    Write-Host "[OK] Synchronized logo.ico from Desktop." -ForegroundColor Green
}

# Detect current version from MawaqitSetup.iss
$issContent = Get-Content "$ScriptDir\MawaqitSetup.iss" -Raw
$appVersion = "2.2.4"
if ($issContent -match '#define MyAppVersion "([^"]+)"') {
    $appVersion = $matches[1]
}
Write-Host "[OK] Target Application Version: $appVersion" -ForegroundColor Green

# Clean stale installer outputs from past versions
$outputDir = Join-Path $ScriptDir "installer_output"
if (Test-Path $outputDir) {
    Get-ChildItem -Path $outputDir -Filter "*.exe" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*$appVersion*" } |
        Remove-Item -Force
} else {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

# 2. Determine build modes (default: build both)
$buildStandard = $true
$buildStandalone = $true

if ($Standalone) {
    $buildStandard = $false
    $buildStandalone = $true
}

# 3. Build Standard (Framework-dependent, ~3 MB)
if ($buildStandard) {
    Write-Host ""
    Write-Host "--> Publishing Standard (Framework-Dependent)..." -ForegroundColor Yellow
    $publishDir = Join-Path $ScriptDir "publish"
    
    & dotnet publish "$ScriptDir\Mawaqit.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $publishDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed for standard release."
        exit $LASTEXITCODE
    }

    # Ensure only Media is present in publish directory
    $pubRes = Join-Path $publishDir "Resources"
    if (-not (Test-Path $pubRes)) { New-Item -ItemType Directory -Path $pubRes -Force | Out-Null }
    Copy-Item -Path "$ScriptDir\Resources\Media" -Destination $pubRes -Recurse -Force
    if (Test-Path "$pubRes\logo.png") { Remove-Item "$pubRes\logo.png" -Force }
    if (Test-Path "$pubRes\logo.ico") { Remove-Item "$pubRes\logo.ico" -Force }
    if (Test-Path "$publishDir\logo.ico") { Remove-Item "$publishDir\logo.ico" -Force }

    Write-Host "--> Compiling Standard Setup via Inno Setup..." -ForegroundColor Yellow
    & "$isccPath" "$ScriptDir\MawaqitSetup.iss"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Inno Setup compilation failed for standard release."
        exit $LASTEXITCODE
    }
}

# 4. Build Standalone (Self-contained, bundled .NET 8, works on any PC)
if ($buildStandalone) {
    Write-Host ""
    Write-Host "--> Publishing Standalone (Self-Contained .NET 8)..." -ForegroundColor Yellow
    $publishScDir = Join-Path $ScriptDir "publish_sc"
    
    & dotnet publish "$ScriptDir\Mawaqit.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o $publishScDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet publish failed for standalone release."
        exit $LASTEXITCODE
    }

    # Ensure only Media is present in standalone publish directory
    $pubScRes = Join-Path $publishScDir "Resources"
    if (-not (Test-Path $pubScRes)) { New-Item -ItemType Directory -Path $pubScRes -Force | Out-Null }
    Copy-Item -Path "$ScriptDir\Resources\Media" -Destination $pubScRes -Recurse -Force
    if (Test-Path "$pubScRes\logo.png") { Remove-Item "$pubScRes\logo.png" -Force }
    if (Test-Path "$pubScRes\logo.ico") { Remove-Item "$pubScRes\logo.ico" -Force }
    if (Test-Path "$publishScDir\logo.ico") { Remove-Item "$publishScDir\logo.ico" -Force }

    Write-Host "--> Compiling Standalone Setup via Inno Setup..." -ForegroundColor Yellow
    & "$isccPath" /DStandalone=1 "$ScriptDir\MawaqitSetup.iss"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Inno Setup compilation failed for standalone release."
        exit $LASTEXITCODE
    }
}

# 5. Output Summary & Copy to Desktop
Write-Host ""
Write-Host "====================================================" -ForegroundColor Green
Write-Host "             BUILD COMPLETE SUCCESSFULLY!" -ForegroundColor Green
Write-Host "====================================================" -ForegroundColor Green

$installers = Get-ChildItem -Path $outputDir -Filter "*$appVersion*.exe" -ErrorAction SilentlyContinue
$desktopDir = [Environment]::GetFolderPath('Desktop')

# Clean old versions from desktop to avoid clutter
if (Test-Path $desktopDir) {
    Get-ChildItem -Path $desktopDir -Filter "Mawaqit-*-Setup.exe" -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -notlike "*$appVersion*" } |
        Remove-Item -Force
}

foreach ($file in $installers) {
    $sizeMb = [math]::Round($file.Length / 1048576, 2)
    Write-Host ("Installer: " + $file.Name + " (" + $sizeMb + " MB)") -ForegroundColor Cyan
    Write-Host ("Location:  " + $file.FullName) -ForegroundColor Gray
    Write-Host ("Timestamp: " + $file.LastWriteTime) -ForegroundColor Gray

    # Copy to Desktop for easy user access
    if (Test-Path $desktopDir) {
        Copy-Item -Path $file.FullName -Destination $desktopDir -Force
        Write-Host "--> Copied to Desktop: $(Join-Path $desktopDir $file.Name)" -ForegroundColor Green
    }
    Write-Host ""
}
