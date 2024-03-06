$shipped = [System.Collections.Generic.SortedSet[string]](Get-Content ./src/OpaDotNet.Wasm/PublicAPI.Shipped.txt)
Get-Content ./src/OpaDotNet.Wasm/PublicAPI.Unshipped.txt | % { $shipped.Add($_) | Out-Null }
$shipped > ./src/OpaDotNet.Wasm/PublicAPI.Shipped.txt
"#nullable enable`n" > ./src/OpaDotNet.Wasm/PublicAPI.Unshipped.txt