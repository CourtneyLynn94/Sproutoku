# Runs the same checks CI runs, in order, and stops at the first failure.
# Run from anywhere:  .\scripts\ci-checks.ps1
# To auto-fix formatting instead of only checking it:  .\scripts\ci-checks.ps1 -Fix

param(
    [switch]$Fix
)

$ErrorActionPreference = 'Stop'

# Run from the repo root (this script lives in scripts\).
Set-Location (Split-Path -Parent $PSScriptRoot)

$verify = if ($Fix) { @() } else { @('--verify-no-changes') }

$steps = @(
    @{ Name = 'Format check: Core';       Args = @('format', 'src/Sproutoku.Core/Sproutoku.Core.csproj') + $verify },
    @{ Name = 'Format check: Core tests'; Args = @('format', 'tests/Sproutoku.Core.Tests/Sproutoku.Core.Tests.csproj') + $verify },
    @{ Name = 'Build (warnings as errors)'; Args = @('build', 'tests/Sproutoku.Core.Tests/Sproutoku.Core.Tests.csproj', '-warnaserror') },
    @{ Name = 'Tests (skipping slow)';    Args = @('test', 'tests/Sproutoku.Core.Tests', '--filter', 'Speed!=Slow') }
)

$i = 0
foreach ($step in $steps) {
    $i++
    Write-Host ""
    Write-Host "[$i/$($steps.Count)] $($step.Name)" -ForegroundColor Cyan
    Write-Host "dotnet $($step.Args -join ' ')" -ForegroundColor DarkGray

    & dotnet @($step.Args)

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "FAILED at step ${i}: $($step.Name)" -ForegroundColor Red
        if (-not $Fix -and $step.Name -like 'Format check*') {
            Write-Host "Tip: run .\scripts\ci-checks.ps1 -Fix to fix formatting, then run it again." -ForegroundColor Yellow
        }
        exit $LASTEXITCODE
    }
}

Write-Host ""
Write-Host "All checks passed. Ready to push." -ForegroundColor Green
