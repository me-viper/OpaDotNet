#! /bin/pwsh
param (
    [string[]]
    [ValidateSet("Cli", "Interop")]
    $Compiler,
    [switch]
    $Sequental,
    [switch]
    $LogToConsole,
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

    foreach ($test in $tests) {
        Write-Host "Testing $test" -ForegroundColor Green

        $traits = $supportedCompilers | ? { $_ -ne $comp } | % { "Compiler!=$_" } | Join-String -Separator '&'

        if ($LogToConsole) {
            $Remaining += '--logger'
            $Remaining += '"console;verbosity=detailed"'
        }

        if ($Sequental) {
            dotnet test -m:1 --filter "$traits" @Remaining $test.FullName
        } else {
            dotnet test --filter "$traits" @Remaining $test.FullName
        }
    }
}