#! /usr/bin/pwsh

$targets = @(
    @{
        OS = "windows";
        Arch= "amd64";
        Ext = "dll";
        CC = "x86_64-w64-mingw32-gcc";
        CXX = "x86_64-w64-mingw32-g++";
    },
    @{
        OS = "linux";
        Arch = "amd64";
        Ext = "so";
        Cc = "gcc";
        Cxx = "g++"
    }
)

if (Test-Path ./lib) {
    Remove-Item ./lib -Recurse
}

$hash = git rev-parse HEAD
Write-Host "SHA: $hash"

$targets | %{
    $outPath = "$($_.OS)-$($_.Arch)"

    Write-Host "Building $outPath...."

    $env:CGO_ENABLED = 1
    $env:GOOS = $_.OS
    $env:GOARCH = $_.Arch
    $env:CC = $_.CC
    $env:CXX = $_.CXX

    $ba = @(
        "-C", "./opa-native"
        "-ldflags", "-w -s -X main.Vcs=$hash",
        "-buildmode=c-shared",
        "-o", "./lib/$outPath/Opa.Interop.$($_.Ext)",
        "./main.go")

    if ($IsWindows) {
        $env:WSLENV = "GOOS/u:GOARCH/u:CGO_ENABLED/u:CC/u:CXX/u"
        wsl /usr/local/go/bin/go build @ba
    } else {
        go build @ba
    }

    if (-not $?) {
        throw "Compilation failed"
    }
}

Write-Host -ForegroundColor Green "Done!"