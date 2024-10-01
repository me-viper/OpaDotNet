#! /bin/pwsh
param (
    [string[]]
    [ValidateSet("Cli", "Interop")]
    $Compiler,
    [switch]
    $Sequental,
    [switch]
    $LogToConsole,
    [string]
    $ExtraFilters,
    [Parameter(ValueFromRemainingArguments)]
    [string[]]
    $Remaining
)

$tests = Get-ChildItem "$PSScriptRoot/../src" -Recurse -Include *.Tests.csproj
$supportedCompilers = @("Cli", "Interop")

if (!$Compiler) {
    $Compiler = $supportedCompilers
}

foreach ($comp in $Compiler)
{
    $env:OPA_TEST_COMPILER = $comp

    Write-Host "Using $env:OPA_TEST_COMPILER compiler" -ForegroundColor Green

    if ($LogToConsole) {
        $Remaining += '--logger'
        $Remaining += '"console;verbosity=detailed"'
    }

    $traits = $supportedCompilers | ? { $_ -ne $comp } | % { "Compiler!=$_" } | Join-String -Separator '&'

    if ($ExtraFilters) {
        $traits += "&$ExtraFilters"
    }

    foreach ($test in $tests) {
        Write-Host "Testing $test" -ForegroundColor Green

        if ($Sequental) {
            dotnet test -m:1 --filter "$traits" @Remaining $test.FullName
        } else {
            dotnet test --filter "$traits" @Remaining $test.FullName
        }

        $exitCode = $LastExitCode
        Write-Host "CODE: $exitCode"

        if ($exitCode -ne 0) {
            throw "Test run failed"
        }
    }
}