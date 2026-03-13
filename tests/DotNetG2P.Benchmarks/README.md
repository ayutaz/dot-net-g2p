# DotNetG2P Benchmarks

Representative BenchmarkDotNet scenarios for English, Chinese, and Korean engines.
The current suite is the baseline for future Japanese, multilingual, and Romance-language benchmark expansion.

## Run

```bash
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --list flat
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*English*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Chinese*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Korean*"
```

BenchmarkDotNet writes reports under `BenchmarkDotNet.Artifacts/`.
