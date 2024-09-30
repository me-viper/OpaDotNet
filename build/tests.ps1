#! /bin/pwsh
param (
    [string]
    [ValidateSet("Cli", "Interop")]
    $Compiler,
    [Parameter(ValueFromRemainingArguments)]
    [string[]]
    $Remaining
)

$tests = Get-ChildItem "$PSScriptRoot/../src" -Recurse -Include *.Tests.csproj

if (!$env:OPA_TEST_COMPILER) {
    $env:OPA_TEST_COMPILER = 'Interop'
}

Write-Host "Using $env:OPA_TEST_COMPILER compiler" -ForegroundColor Green

foreach ($test in $tests) {
    Write-Host "Testing $test" -ForegroundColor Green
    # --filter "Compiler=$env:OPA_TEST_COMPILER"
    dotnet test -m:1 @Remaining $test.FullName
}