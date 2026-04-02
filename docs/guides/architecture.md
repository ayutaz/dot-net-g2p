# Architecture Overview

DotNetG2P keeps each language engine independently consumable.
Japanese has the deepest processing pipeline, while the other languages follow lighter rule-based flows.
The multilingual package sits on top and routes segmented text into the appropriate engine.

## High-Level Principles

- Keep public APIs language-shaped rather than forcing artificial unification.
- Share only small internal building blocks such as batch helpers and contributor tooling.
- Prefer pure C# and embedded resources so the packages remain easy to ship in Unity and standard .NET apps.
- Use CI to enforce quality gates around tests, package validation, DocFX builds, trim/AOT publish checks, and SBOM generation.

See the Architecture section in `CONTRIBUTING.md` for the repo-level breakdown and the rationale behind package boundaries.
