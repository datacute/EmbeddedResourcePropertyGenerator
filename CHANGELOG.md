# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Generated properties now use the C# `field` keyword on compilers that support it (C# 14+), eliminating explicit backing fields

## [1.1.1] - 2025-08-03

### Added
- Included a count of the matching embedded resources in the generated class doc-comments

### Changed
- Converted to use Datacute.IncrementalGeneratorExtensions and Datacute.AdditionalTextConstantGenerator

### Fixed
- Fixed nullable reference type support for older .NET versions that don't support nullable annotations
- Removed file-scoped namespace from attribute for better compatibility with older C# versions

## [1.1.0] - 2025-06-29

### Added
- Support for nested classes and generics
- Automatic inclusion of EmbeddedResources as Additional Text Files to make integration easier

### Changed
- Rewrote the generator pipeline to improve performance and only regenerate when additional texts are changed
- Lowered the minimum version of Microsoft.CodeAnalysis.CSharp required to support older .NET versions

### Fixed
- Resolves issue #4 (performance improvements)
- Resolves issue #3 (nested classes and generics support)

## [1.0.0] - 2024-10-28

### Added
- First stable release

## [0.0.1-alpha.6b] - 2024-09-24

### Fixed
- Corrected the published package (but the new version broke the doc links)

## [0.0.1-alpha.6] - 2024-09-24

### Added
- More documentation

### Changed
- Removed windows specific paths from tests

## [0.0.1-alpha.5] - 2024-09-22

### Fixed
- Included doc-comments of the EmbeddedResourceProperties attribute in the package

## [0.0.1-alpha.4] - 2024-09-15

### Added
- Read method in each class
- The Read method, backing fields, and resource names are now available to the class

### Changed
- Moved the attribute to its own dll

## [0.0.1-alpha.3] - 2024-09-14

### Added
- Included `EmbeddedResourcePropertyExample` project in github repository

### Fixed
- Removing doc duplication

## [0.0.1-alpha.2] - 2024-09-13

### Added
- `[Conditional]` attribute to restrict inclusion of the usage of the `EmbeddedResourcePropertiesAttribute` in the output

## [0.0.1-alpha] - 2024-09-09

### Added
- Support for overriding `ReadEmbeddedResourceValue` partial method

[Unreleased]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/compare/1.1.1...HEAD
[1.1.1]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/releases/tag/1.1.1
[1.1.0]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/releases/tag/1.1.0
[1.0.0]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/releases/tag/1.0.0
[0.0.1-alpha.6b]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/releases/tag/0.0.1-alpha.6b
[0.0.1-alpha.6]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/releases/tag/0.0.1-alpha.6
[0.0.1-alpha.5]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/releases/tag/0.0.1-alpha.5
[0.0.1-alpha.4]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/releases/tag/0.0.1-alpha.4
[0.0.1-alpha.3]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/releases/tag/0.0.1-alpha.3
[0.0.1-alpha.2]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/releases/tag/0.0.1-alpha.2
[0.0.1-alpha]: https://github.com/datacute/EmbeddedResourcePropertyGenerator/releases/tag/0.0.1-alpha