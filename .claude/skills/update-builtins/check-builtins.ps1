#Requires -Version 7
<#
.SYNOPSIS
    Reports where docs/articles/Builtins.md is out of sync with the code and with the pinned OPA version.

.DESCRIPTION
    Read-only. Compares three sources:
      - OPA built-ins at the version pinned in src/Compilation/Interop/opa-native/go.mod (capabilities JSON), minus
        those OPA compiles natively into WASM (internal/compiler/wasm/wasm.go) - these need a .NET implementation;
      - built-ins implemented in src/Wasm/src/DefaultOpaImportsAbi*.cs ("name" => ... switch arms);
      - rows of docs/articles/Builtins.md.
    With -CheckLinks it also verifies every row's link against anchors on the live OPA docs pages.

.PARAMETER OpaVersion
    OPA version to compare against. Defaults to the version pinned in go.mod.

.PARAMETER CheckLinks
    Fetch OPA's built-in docs pages and report rows whose link page/anchor doesn't exist.
#>
param(
    [string]$OpaVersion,
    [switch]$CheckLinks
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../../..").Path
$docPath = Join-Path $root 'docs/articles/Builtins.md'
$raw = 'https://raw.githubusercontent.com/open-policy-agent/opa'
$docsBase = 'https://www.openpolicyagent.org/docs/policy-reference/builtins'

if (-not $OpaVersion) {
    $goMod = Get-Content (Join-Path $root 'src/Compilation/Interop/opa-native/go.mod') -Raw
    $OpaVersion = [regex]::Match($goMod, 'github\.com/open-policy-agent/opa (v[\d.]+)').Groups[1].Value
}

$doc = Get-Content $docPath
$docVersion = ($doc | Select-String 'as for version (v[\d.]+)').Matches[0].Groups[1].Value

# Doc rows: name -> @{ Done; Link }
$rows = [ordered]@{}
foreach ($line in $doc) {
    $m = [regex]::Match($line, '^\|\s*(?:\[(?<n>[a-z0-9_.]+)\]\((?<l>[^)]*)\)|(?<n>[a-z0-9_.]+))\s*\|\s*(?<s>[^|]*?)\s*\|')
    if ($m.Success) {
        $rows[$m.Groups['n'].Value] = @{ Done = $m.Groups['s'].Value -eq ':white_check_mark:'; Link = $m.Groups['l'].Value }
    }
}

# Implemented in .NET
$implemented = Get-ChildItem (Join-Path $root 'src/Wasm/src') -Filter 'DefaultOpaImportsAbi*.cs' |
    Select-String '^\s*"(?<n>[a-z0-9_.]+)"\s*=>' -AllMatches |
    ForEach-Object { $_.Matches.Groups | Where-Object Name -EQ 'n' | ForEach-Object Value } |
    Sort-Object -Unique

function Get-OpaBuiltins([string]$version) {
    $caps = Invoke-RestMethod "$raw/$version/capabilities/$version.json"
    $caps.builtins.name | Sort-Object -Unique
}

# Built-ins that never reach the wasm host: deprecated ones, infix operators (assign, eq, ...) and print, which the
# compiler rewrites to internal.print.
function Get-OpaHostBuiltins([string]$version) {
    $caps = Invoke-RestMethod "$raw/$version/capabilities/$version.json"
    $caps.builtins | Where-Object { -not $_.deprecated -and -not $_.infix -and $_.name -ne 'print' } |
        ForEach-Object name | Sort-Object -Unique
}

# Built-ins OPA compiles into WASM itself: map ast.<Var>.Name keys in wasm.go to names via v1/ast/builtins.go.
$wasmGo = Invoke-RestMethod "$raw/$OpaVersion/internal/compiler/wasm/wasm.go"
$astGo = Invoke-RestMethod "$raw/$OpaVersion/v1/ast/builtins.go"
$astNames = @{}
foreach ($m in [regex]::Matches($astGo, 'var (\w+) = &Builtin\{\s*Name:\s*"([^"]+)"')) {
    $astNames[$m.Groups[1].Value] = $m.Groups[2].Value
}
$builtinsMap = [regex]::Match($wasmGo, '(?s)var builtinsFunctions = map\[string\]string\{(.*?)\n\}').Groups[1].Value
$native = [regex]::Matches($builtinsMap, 'ast\.(\w+)\.Name') |
    ForEach-Object { $astNames[$_.Groups[1].Value] } | Where-Object { $_ } | Sort-Object -Unique

$opa = Get-OpaBuiltins $OpaVersion
$needsDotNet = Get-OpaHostBuiltins $OpaVersion | Where-Object { $_ -notin $native -and $_ -notlike 'internal.*' }
# Switch arms that aren't built-in names (e.g. unit suffixes) are dropped by intersecting with OPA's list.
$implemented = $implemented | Where-Object { $_ -in $opa }

function Write-Section([string]$title, $items) {
    $items = @($items | Where-Object { $_ })
    Write-Host "`n== $title ($($items.Count))" -ForegroundColor Cyan
    $items | ForEach-Object { Write-Host "  $_" }
}

Write-Host "Doc header version: $docVersion; OPA version: $OpaVersion"
if ($docVersion -ne $OpaVersion) {
    $old = Get-OpaBuiltins $docVersion
    Write-Section "Added in OPA since $docVersion" ($opa | Where-Object { $_ -notin $old } | ForEach-Object {
        $_ + $(if ($_ -in $native) { '  (native wasm, no row needed)' } elseif ($rows.Contains($_)) { '  (already in doc)' } else { '' })
    })
    Write-Section "Removed from OPA since $docVersion" ($old | Where-Object { $_ -notin $opa })
}

Write-Section 'Need .NET implementation but missing from doc' ($needsDotNet | Where-Object { -not $rows.Contains($_) })
Write-Section 'Implemented but missing from doc' ($implemented | Where-Object { $_ -notlike 'internal.*' -and -not $rows.Contains($_) })
Write-Section 'Marked done in doc but not implemented' ($rows.Keys | Where-Object { $rows[$_].Done -and $_ -notin $implemented })
Write-Section 'Implemented but marked missing in doc' ($rows.Keys | Where-Object { -not $rows[$_].Done -and $_ -in $implemented })
Write-Section "In doc but not an OPA $OpaVersion built-in" ($rows.Keys | Where-Object { $_ -notin $opa })

if ($CheckLinks) {
    $index = Invoke-WebRequest $docsBase
    $pages = [regex]::Matches($index.Content, 'href="/docs/policy-reference/builtins/([a-z0-9_.-]+)"') |
        ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -ne '.md' } | Sort-Object -Unique
    $anchors = @{}
    foreach ($page in $pages) {
        try { $html = (Invoke-WebRequest "$docsBase/$page").Content } catch { Write-Host "  page $page unavailable"; continue }
        foreach ($m in [regex]::Matches($html, 'id="(builtin-[a-z0-9_.-]+)"')) { $anchors["$page#$($m.Groups[1].Value)"] = $true }
    }
    Write-Section 'Rows with broken or missing links' ($rows.Keys | Where-Object {
        $link = $rows[$_].Link
        -not $link.StartsWith("$docsBase/") -or -not $anchors.Contains($link.Substring($docsBase.Length + 1))
    } | ForEach-Object { "$_ -> $($rows[$_].Link)" })
}
