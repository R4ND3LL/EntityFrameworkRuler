#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Local simulation of GitHub Actions CI workflow

.DESCRIPTION
    Mirrors the build and test steps from .github/workflows/dotnet.yml
    Run this before pushing to catch CI issues locally.

.PARAMETER SkipClean
    Skip removing nupkg folder

.PARAMETER SkipPack
    Skip NuGet package creation

.EXAMPLE
    .\Test-CI.ps1
    Run full CI simulation

.EXAMPLE
    .\Test-CI.ps1 -SkipPack
    Run build and tests only, skip packaging
#>

param(
    [switch]$SkipClean,
    [switch]$SkipPack
)

$ErrorActionPreference = "Stop"

function Write-Step {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Test-LastExitCode {
    param([string]$Step)
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n❌ FAILED: $Step" -ForegroundColor Red
        Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "✅ SUCCESS: $Step`n" -ForegroundColor Green
}

Write-Host @"
╔══════════════════════════════════════════════════════════╗
║                                                          ║
║         Local CI Simulation for EntityFrameworkRuler    ║
║                                                          ║
╚══════════════════════════════════════════════════════════╝
"@ -ForegroundColor Yellow

# Step 1: Restore dependencies
Write-Step "Restoring dependencies"
dotnet restore EntityFrameworkRuler.sln --v:m
Test-LastExitCode "dotnet restore"

# Step 2: Test Build (Debug)
Write-Step "Building in Debug configuration"
dotnet build EntityFrameworkRuler.sln --configuration Debug --no-restore --v:q
Test-LastExitCode "dotnet build (Debug)"

# Step 3: Test Design
Write-Step "Running EntityFrameworkRuler.Design.Tests"
dotnet test ./Tests/EntityFrameworkRuler.Design.Tests --no-build --v:m
Test-LastExitCode "Design Tests"

# Step 4: Test CLI
Write-Step "Running EntityFrameworkRuler.Tests"
dotnet test ./Tests/EntityFrameworkRuler.Tests --no-build --v:m
Test-LastExitCode "CLI Tests"

# Step 5: Remove nupkg folder
if (-not $SkipClean) {
    Write-Step "Removing nupkg folder"
    if (Test-Path "./nupkg") {
        Remove-Item -Path "./nupkg" -Recurse -Force
        Write-Host "✅ SUCCESS: Removed nupkg folder`n" -ForegroundColor Green
    } else {
        Write-Host "ℹ️  nupkg folder doesn't exist, skipping`n" -ForegroundColor Yellow
    }
}

# Step 6: Release Build
Write-Step "Building in Release configuration"
dotnet build ./EntityFrameworkRuler.sln --configuration Release --no-restore --v:q
Test-LastExitCode "dotnet build (Release)"

# Step 7: Create NuGet packages
if (-not $SkipPack) {
    Write-Step "Creating NuGet packages"
    dotnet pack --configuration Release --no-build --no-restore ./EntityFrameworkRuler.sln --property:PackageOutputPath="$PWD\nupkg"
    Test-LastExitCode "dotnet pack"

    if (Test-Path "./nupkg") {
        Write-Host "`nPackages created:" -ForegroundColor Cyan
        Get-ChildItem "./nupkg/*.nupkg" | ForEach-Object {
            Write-Host "  📦 $($_.Name)" -ForegroundColor Green
        }
    }
}

Write-Host @"

╔══════════════════════════════════════════════════════════╗
║                                                          ║
║  ✅ ALL CI STEPS PASSED!                                ║
║                                                          ║
║  Your changes are ready to push to GitHub               ║
║                                                          ║
╚══════════════════════════════════════════════════════════╝

"@ -ForegroundColor Green

# Note: This script doesn't include:
# - VSIX build steps (require specific VS setup)
# - Publishing to NuGet (requires NUGET_TOKEN secret)
# - Publishing to VS Marketplace (requires VSIX_TOKEN secret)
