#! /bin/pwsh
param (
    [string]
    $Filter = "*",
    [switch]
    $SkipBench,
    [switch]
    $SkipStat,
    [Parameter(ValueFromRemainingArguments)]
    [string[]]
    $Remaining
)

$base = "BenchmarkDotNet.Artifacts"
$stat = "./$base/stat"

if (-not $SkipBench) {
    dotnet run -c Release --property WarningLevel=0 --filter=$Filter @Remaining
}

if (-not $SkipStat) {
    if (-not (Test-Path $stat)) {
        mkdir $stat
    }

    $semVer = $(dotnet nbgv get-version -v Version)
    Write-Host $semVer
    $ver = [System.Version]::Parse($semVer)
    $verPrefix = "v{0}.{1}" -f $ver.Major, $ver.Minor

    $src = Get-ChildItem $base -rec -Include "*-report.txt"

    foreach ($s in $src) {
        $name = $s.Name -replace "-report.txt", ""
        $dest = "$stat/$name"

        if (-not (Test-Path "$dest")) {
            mkdir $dest
        }

        $ts = $s.LastWriteTimeUtc.ToString("yyyyMMdd-HHmmss")

        Copy-Item -Path $s -Destination "$dest/$verPrefix-$ts.txt"
    }

    $results = $(Get-ChildItem $stat -Directory | ? { $_.NameString -like $Filter })

    foreach ($r in $results) {
        Push-Location $r

        $reports = @(Get-ChildItem | % { $_.Name })
        benchstat @reports

        Pop-Location
    }
}