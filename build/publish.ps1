#! /bin/pwsh

param (
    [string]
    $ApiKey = $null,
    [string[]]
    $Include = $null,
    [switch]
    $SkipBuild
)

$nugetApiKey = $ApiKey

if (!$nugetApiKey) {
    $nugetApiKey = $env:NUGET_API_KEY
}

if (!$SkipBuild) {
    if (Test-Path $PSScriptRoot/../artifacts/package) {
        Remove-Item $PSScriptRoot/../artifacts/package -Recurse -Force
    }

    dotnet pack -c Release $PSScriptRoot/../OpaDotNet.slnx

    if (!$?) {
        throw 'Build failed'
    }
}

if (!$Include) {
    dotnet nuget push "./artifacts/*.nupkg" -k $nugetApiKey -s https://api.nuget.org/v3/index.json --skip-duplicate
    dotnet nuget push "./artifacts/*.snupkg" -k $nugetApiKey -s https://api.nuget.org/v3/index.json --skip-duplicate
} else {
    foreach ($inc in $Include) {
        dotnet nuget push "./artifacts/$inc.nupkg" -k $nugetApiKey -s https://api.nuget.org/v3/index.json --skip-duplicate
        dotnet nuget push "./artifacts/$inc.snupkg" -k $nugetApiKey -s https://api.nuget.org/v3/index.json --skip-duplicate
    }
}