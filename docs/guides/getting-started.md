# Getting Started

## Prerequisites

- .NET SDK 9.0 or later for the main contributor workflow
- PowerShell 7 for helper scripts
- `naist-jdic` for Japanese and multilingual scenarios

## Common Commands

```bash
dotnet restore DotNetG2P.slnx
dotnet build DotNetG2P.slnx --configuration Release
dotnet test DotNetG2P.slnx --configuration Release --filter "Category!=Performance"
dotnet tool restore
dotnet tool run docfx docs/docfx.json
dotnet tool run dotnet-CycloneDX DotNetG2P.slnx -o ./artifacts/sbom -t --disable-hash-computation
```

## Sample Applications

- `samples/DotNetG2P.Console/` demonstrates Japanese, multilingual, and standalone language engines.
- `tests/DotNetG2P.Benchmarks/` contains BenchmarkDotNet scenarios for the supported benchmark suites.
- `tests/DotNetG2P.PublishSmoke/` exercises trim and NativeAOT publish compatibility.

## Dictionary Setup

Install the Japanese dictionary when you need Japanese or multilingual coverage:

```powershell
pwsh -File tools/install_naist_jdic.ps1
```

The engines also honor `DOTNETG2P_NAIST_JDIC_PATH` and `NAIST_JDIC_PATH`.
