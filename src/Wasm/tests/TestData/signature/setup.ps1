#! /bin/pwsh

pushd $PSScriptRoot/source

opa sign ./ -b --signing-alg RS256 --signing-key $PSScriptRoot/rsa.pem -o $PSScriptRoot

opa build ./ -t wasm -b --signing-alg RS256 --signing-key $PSScriptRoot/rsa.pem -o $PSScriptRoot/ok.tar.gz

#opa build ./ -t wasm -b --signing-alg RS256 --signing-key $PSScriptRoot/rsa.pem --scope test -o $PSScriptRoot/scope.tar.gz

popd

tar -xzvf $PSScriptRoot/ok.tar.gz "/policy.wasm"