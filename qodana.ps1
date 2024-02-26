#! /bin/pwsh

param (
    [string]
    $Token,
    [string]
    $Image = "jetbrains/qodana-cdnet:2023.3-eap"
)

$ErrorActionPreference = "Stop"

$token = $Token ?? $env:QODANA_TOKEN

if (Test-Path qodana.sln) {
    rm qodana.sln
}

dotnet new solution -n qodana
dotnet sln qodana.sln add ./src/OpaDotNet.Wasm/OpaDotNet.Wasm.csproj --in-root

docker run -v ${PWD}:/data/project/ -e QODANA_TOKEN="$token" $Image --solution qodana.sln

if (Test-Path qodana.sln) {
    rm qodana.sln
}