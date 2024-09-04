#!/usr/bin/env pwsh
param (
    [string]
    $Path = $null
)

if (!$Path) {
    $Path = $PSScriptRoot
}

$unshipped = Get-ChildItem * -Include "PublicAPI.Unshipped.txt" -Recurse

foreach ($api in $unshipped) {
    $basePath = Split-Path $api.FullName -Parent

    $shipped = [System.Collections.Generic.SortedSet[string]](Get-Content "$basePath/PublicAPI.Shipped.txt")
    Get-Content $api | % { $shipped.Add($_) | Out-Null }
    $shipped > "$basePath/PublicAPI.Shipped.txt"
    "#nullable enable`n" > $api.FullName
}