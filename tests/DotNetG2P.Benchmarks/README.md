# DotNetG2P Benchmarks

Representative BenchmarkDotNet scenarios for English, Chinese, and Korean engines.

## Run

```bash
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*English*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Chinese*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Korean*"
```
