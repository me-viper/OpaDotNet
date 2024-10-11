Get-ChildItem version.json -Recurse `
    | % { Write-Host "$_" -ForegroundColor Green; dotnet nbgv get-version --project $_.Directory.FullName }