Changelog
--- 

## v1.1.0
Release Date: 2025-06-29

### Features
- Rewrote the generator pipeline to improve performance, and only regenerate when additional texts are changed (resolves #4)
- Added support for nested classes and generics (resolves #3)
- Made integration easier by automatically including EmbeddedResources as Additional Text Files
- Lowered the minimum version of Microsoft.CodeAnalysis.CSharp required to support older .NET versions

**Full Changelog**: https://github.com/datacute/EmbeddedResourcePropertyGenerator/compare/1.0.0...1.1.0

## v1.0.0
Release Date: 2024-10-28

### Features

* First stable release

## v0.0.1-alpha.6b
Release Date: 2024-09-24

### Fixes
* Corrected the published package (but the new version broke the doc links)

## v0.0.1-alpha.6
Release Date: 2024-09-24

### Features

* Removed windows specific paths from tests
* More documentation

## v0.0.1-alpha.5
Release Date: 2024-09-22

### Fixes

* Included doc-comments of the EmbeddedResourceProperties attribute in the package

## v0.0.1-alpha.4
Release Date: 2024-09-15

### Features

* Moved the attribute to its own dll
* Include Read method in each class
* The Read method, backing fields, and resource names are available to the class

## v0.0.1-alpha.3
Release Date: 2024-09-14

### Features

* Included `EmbeddedResourcePropertyExample` project in github repository

### Fixes

* Removing doc duplication

## v0.0.1-alpha.2
Release Date: 2024-09-13

### Features

* Add `[Conditional]` attribute to restrict inclusion of the usage of 
  the `EmbeddedResourcePropertiesAttribute` in the output.

## v0.0.1-alpha
Release Date: 2024-09-09

### Features

* Add support for overriding `ReadEmbeddedResourceValue` partial method
