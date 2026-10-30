#! /bin/pwsh
<#
.SYNOPSIS
    Syncs OPA compatibility test cases (src/Wasm/tests/SdkV1/v1) with the upstream OPA test suite.
.PARAMETER Ref
    OPA git tag or branch to sync from. Defaults to the OPA version referenced by the Interop native library go.mod.
#>
param (
    [string]
    $Ref = $null
)

$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$target = Join-Path $root 'src/Wasm/tests/SdkV1/v1'
$upstreamPath = 'v1/test/cases/testdata/v1'

if (!$Ref) {
    $goMod = Get-Content (Join-Path $root 'src/Compilation/Interop/opa-native/go.mod')
    $match = $goMod | Select-String 'github.com/open-policy-agent/opa (v\S+)' | Select-Object -First 1

    if (!$match) {
        throw 'Failed to determine OPA version from go.mod. Specify -Ref explicitly.'
    }

    $Ref = $match.Matches[0].Groups[1].Value
}

Write-Host "Syncing OPA test cases from $Ref" -ForegroundColor Green

$temp = Join-Path ([System.IO.Path]::GetTempPath()) "opa-tests-$([guid]::NewGuid().ToString('N'))"

try {
    # Keep upstream LF line endings, otherwise unchanged files show up as modified.
    git -c advice.detachedHead=false clone --quiet --depth 1 --filter=blob:none --sparse --branch $Ref `
        --config core.autocrlf=false `
        https://github.com/open-policy-agent/opa.git $temp

    if ($LASTEXITCODE -ne 0) {
        throw "git clone failed for ref $Ref"
    }

    git -C $temp sparse-checkout set $upstreamPath

    if ($LASTEXITCODE -ne 0) {
        throw 'git sparse-checkout failed'
    }

    $source = Join-Path $temp $upstreamPath

    if (!(Test-Path $source)) {
        throw "$upstreamPath not found in $Ref"
    }

    if (Test-Path $target) {
        Remove-Item $target -Recurse -Force
    }

    Copy-Item $source $target -Recurse
}
finally {
    if (Test-Path $temp) {
        Remove-Item $temp -Recurse -Force
    }
}

# Stage to refresh the index, so only actual changes (including added/removed files) are reported.
git -C $root add --all -- $target
git -C $root status --short -- $target
