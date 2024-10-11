#! /bin/pwsh
param (
    [Parameter(ValueFromRemainingArguments)]
    [string[]]
    $Remaining
)

./src/Compilation/Interop/build.ps1

dotent build