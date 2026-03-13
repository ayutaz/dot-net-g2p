# Migration Guide

This file tracks upgrade notes for user-visible changes in DotNetG2P packages and repository workflows.

## Current Status

No released version currently requires a breaking migration step.
Use this guide to record future package, runtime, or setup changes that affect consumers.

## Compatibility Notes

### Japanese And Multilingual Dictionary Requirement

`DotNetG2P` and Japanese-enabled `DotNetG2P.Multilingual` flows still require a `naist-jdic` dictionary install.
This remains an external setup step and should be called out in release notes when package guidance changes.

### Batch API Collection Contracts

Batch APIs are documented by their public return types.
If a method returns `IReadOnlyList<T>`, callers should not depend on a specific concrete runtime collection type.

### SDK And CI Baseline

The repository keeps the root contributor workflow on `DotNetG2P.slnx`, which requires an SDK with SLNX support.
CI also validates the build and test flow on .NET 8 by using project files directly instead of the root solution file.

## Future Entries

When a release introduces a breaking change, add an entry with:

1. The version number and release date.
2. What changed for consumers.
3. Required code or setup updates.
4. Verification steps after migration.
