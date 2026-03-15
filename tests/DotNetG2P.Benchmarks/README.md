# DotNetG2P Benchmarks

Representative BenchmarkDotNet scenarios for Japanese, English, Chinese, Korean, multilingual, and Romance-language engines.
Japanese and multilingual benchmarks require `naist-jdic` through the default lookup locations or `DOTNETG2P_NAIST_JDIC_PATH` / `NAIST_JDIC_PATH`.

## Run

```bash
dotnet tool restore
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --list flat
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Japanese*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*English*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Chinese*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Korean*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Multilingual*"
dotnet run -c Release --project tests/DotNetG2P.Benchmarks -- --filter "*Romance*"
```

BenchmarkDotNet writes reports under `BenchmarkDotNet.Artifacts/`.
