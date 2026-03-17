# DotNetG2P Documentation

DotNetG2P provides pure C# grapheme-to-phoneme engines for Japanese, English, Chinese, Korean, Spanish, French, Portuguese, and mixed-language routing.

## Documentation Areas

- [Getting Started](guides/getting-started.md)
- [Architecture Overview](guides/architecture.md)
- [API Reference](api/toc.yml)

## Repository Documents

- Root README files explain package selection and basic usage.
- `CONTRIBUTING.md` covers the contributor workflow.
- `ARCHITECTURE.md` describes the repo-level structure and package boundaries.

## Notes

- Japanese and multilingual features require `naist-jdic`.
- API metadata is generated from the package projects and XML documentation files.
- The CI validation lane builds this DocFX site together with trim/AOT smoke tests, package validation, and SBOM generation.
