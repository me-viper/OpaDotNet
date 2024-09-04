# DotNet references validation

This sample demonstrates simple use case when we want to ensure dotnet projects reference only "approved" nuget
packages.

## The Sample

Run:

```sh
dotnet run ./data/good.csproj
```

Output:

```sh
All good!
```

Run:

```sh
dotnet run ./data/bad.csproj
```

Output:

```sh
The following packages failed policy validation:
System.Some.Package: 5.0.0
Untrusted.Package: 7.0.0
```
