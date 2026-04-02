# DotNetG2P Documentation

DotNetG2P provides pure C# grapheme-to-phoneme engines for Japanese, English, Chinese, Korean, Spanish, French, Portuguese, Swedish, and mixed-language routing.

## Documentation Areas

- [API Reference](api/toc.yml)

## Repository Documents

- Root README files explain package selection and basic usage.
- `CONTRIBUTING.md` covers the contributor workflow, architecture, and package boundaries.
- `CHANGELOG.md` tracks release history.

## Notes

- Japanese and multilingual features require `naist-jdic`.
- API metadata is generated from the package projects and XML documentation files.
- The CI validation lane builds this DocFX site together with trim/AOT smoke tests, package validation, and SBOM generation.
