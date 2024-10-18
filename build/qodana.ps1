#! /bin/pwsh

param (
    [string]
    $Token,
    [string]
    $Image = "jetbrains/qodana-cdnet:latest",
    [switch]
    $SkipScan
)

$ErrorActionPreference = "Stop"

$token = $Token ?? $env:QODANA_TOKEN

$basePath = Resolve-Path "$PSScriptRoot/.."
$slnPath = "$basePath/qodana.sln"

if (Test-Path $slnPath) {
    rm $slnPath
}

dotnet new solution -n qodana -o "$basePath"

Get-ChildItem "$basePath/src" -Include *.csproj -Recurse `
    | ? { Select-Xml -Path $_.FullName -XPath "//PropertyGroup/IsPackable[.='true']" }
    | % { dotnet sln $slnPath add $_.FullName --in-root }

if (-not $SkipScan) {
    docker run -v ${basePath}:/data/project/ -e QODANA_TOKEN="$token" $Image --solution "qodana.sln"

    if (Test-Path $slnPath) {
        rm $slnPath
    }
}