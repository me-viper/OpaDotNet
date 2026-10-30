#! /bin/pwsh
<#
.SYNOPSIS
    Updates the OPA dependency of the Interop native library (src/Compilation/Interop/opa-native/go.mod).
.DESCRIPTION
    Runs `go get` + `go mod tidy` in the opa-native module and reports the old and new OPA and Go versions.
    It doesn't rebuild the native library or sync OPA test cases; see the follow-up steps it prints.
.PARAMETER Version
    OPA version to update to (e.g. v1.20.0). Defaults to the latest release.
#>
param (
    [string]
    $Version = 'latest'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$module = Join-Path $root 'src/Compilation/Interop/opa-native'
$goModPath = Join-Path $module 'go.mod'
$opaModule = 'github.com/open-policy-agent/opa'

function Get-GoModVersions {
    $goMod = Get-Content $goModPath -Raw
    $opa = [regex]::Match($goMod, "(?m)^\s*(?:require\s+)?$([regex]::Escape($opaModule)) (v\S+)")

    if (!$opa.Success) {
        throw "Failed to determine OPA version from $goModPath"
    }

    @{
        Opa = $opa.Groups[1].Value
        Go = [regex]::Match($goMod, '(?m)^go (\S+)').Groups[1].Value
    }
}

if (!(Get-Command go -ErrorAction SilentlyContinue)) {
    throw 'go not found in PATH. Install Go (see src/Compilation/README.md).'
}

if ($Version -ne 'latest' -and !$Version.StartsWith('v')) {
    $Version = "v$Version"
}

$before = Get-GoModVersions
Write-Host "Updating $opaModule from $($before.Opa) to $Version" -ForegroundColor Green

Push-Location $module

try {
    go get "$opaModule@$Version"

    if ($LASTEXITCODE -ne 0) {
        throw "go get $opaModule@$Version failed"
    }

    go mod tidy

    if ($LASTEXITCODE -ne 0) {
        throw 'go mod tidy failed'
    }
}
finally {
    Pop-Location
}

$after = Get-GoModVersions

if ($after.Opa -eq $before.Opa) {
    Write-Host "OPA is already at $($after.Opa)" -ForegroundColor Yellow
}
else {
    Write-Host "OPA: $($before.Opa) -> $($after.Opa)" -ForegroundColor Green
}

if ($after.Go -ne $before.Go) {
    Write-Host "Go directive: $($before.Go) -> $($after.Go) (CI picks it up via setup-go go-version-file)" `
        -ForegroundColor Yellow
}

git -C $root status --short -- $module

if ($after.Opa -ne $before.Opa) {
    Write-Host @"

Next steps:
  ./build/sync-opa-tests.ps1                  # sync OPA test cases to $($after.Opa)
  ./src/Compilation/Interop/build.ps1         # rebuild the native library
  Update docs/articles/Builtins.md and CHANGELOG.md for the new OPA version
"@
}
