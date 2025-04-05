[![Build](https://github.com/datacute/EmbeddedResourcePropertyGenerator/actions/workflows/ci.yml/badge.svg)](https://github.com/datacute/EmbeddedResourcePropertyGenerator/actions/workflows/ci.yml)

# Embedded Resource Property Generator
The Embedded Resource Property Generator is an Incremental Source Generator
which generates properties for text file embedded resources in a
project, in a similar way to how properties are generated from the string
resources in .resx files.

By adding the `[EmbeddedResourceProperties]` attribute to a partial class,
and specifying the filename extension and folder path to search, the
source generator will add a property to the class for each matching
embedded resource file. Accessing the property will return the text
content of the embedded resource file.

## Why not just use a resx file?
The use case for this project is when you have a large number of text
files that you want to embed in your project, such as SQL queries, and you
wish to  develop these files with an external editor that supports syntax
highlighting, execution, debugging, and other features.

While resx files do support including files, doing so requires either
the use of another custom editor, or manipulating the xml of the resx
file directly.

## What's wrong with just using Assembly.GetManifestResourceStream?
The purpose of providing properties to access the text, is to aid the
developer by generating doc-comments for the properties, showing the first
few lines of the file.

```csharp
/// <summary>Text value of the Embedded Resource: GoodIndenting.cypher</summary>
/// <value>
/// <code>
/// MERGE (n)
///   ON CREATE SET n.prop = 0
/// MERGE (a:A)-[:T]-(b:B)
///   ON CREATE SET a.name = 'me'
///   ON MATCH SET b.name = 'you'
/// RETURN a.prop
/// </code>
/// </value>
/// <remarks>
/// The value is read from the embedded resource on first access.
/// </remarks>
public static string GoodIndenting => 
  // Generated code to read the resource "Namespace.ClassName.GoodIndenting.cypher";
```

The full names of the embedded resources are also generated, and no longer
need to be supplied by the developer, making it easy to move the location
of the resource files to another directory, without needing to find and
fix all the references to the resource names.

## Usage

1. Add the [Datacute.EmbeddedResourcePropertyGenerator NuGet package](https://www.nuget.org/packages/Datacute.EmbeddedResourcePropertyGenerator)
to your project.
    ```bash
    dotnet add package Datacute.EmbeddedResourcePropertyGenerator
    ```
2. Add a directory to your project to group the files you want to embed.
3. Add text files to your project, in that directory, and set their Build
   Action to `EmbeddedResource`.
    ```text
    > SqlQueries
        SelectAll.sql
        SelectById.sql
    Program.cs
    ```
    ```xml
      <ItemGroup>
        <EmbeddedResource Include="SqlQueries\SelectAll.sql" />
        <EmbeddedResource Include="SqlQueries\SelectById.sql"/>
      </ItemGroup>
    ```
4. Add a partial class to your code.
5. Include a using statement to reference the `Datacute.EmbeddedResourcePropertyGenerator` namespace.
6. Put the attribute `[EmbeddedResourceProperties]` on the class.
7. Specify the extension and folder path to search for embedded resources.
8. Use the properties generated on the partial class.
    ```csharp
    using Datacute.EmbeddedResourcePropertyGenerator;
    
    [EmbeddedResourceProperties(".sql", "SqlQueries")]
    public static partial class SqlQuery;
    
    class Program
    {
        static void Main()
        {
            Console.WriteLine(SqlQuery.SelectAll);
        }
    }
    ``` 

Within your .csproj file, you can alter the `PackageReference` to include `PrivateAssets="all" ExcludeAssets="runtime"`
This stops projects that reference this one from also getting a reference to this package, and stops the dll files
from being copied to your build output.

## Localisation and External Overrides
Localisation and External Overrides are not supported. If you need these
features, consider using a resx file instead.

## Non-text File Types
This project expects text files so that it can generate properties that
are strings. It currently expects UTF-8 encoded files.

If you need to embed other types of files, use the 
`Assembly.GetManifestResourceStream` method directly.

## Extending the Behaviour
The generated code includes a private nested class `EmbeddedResource` containing:

| Method or Class | Purpose |
|-----------------|---------|
| `Read(string resourceName)` | Method for reading embedded resources |
| `BackingField` | Nested class caching the property values |
| `ResourceName` | Nested class holding the resource names |

The implementation supports including two ***partial methods*** that can
be implemented in the same partial class as the generated properties.

If the partial methods are not implemented, the calls to them are removed, 
and the code effectively reduces to:

```csharp
public static string Example =>
        EmbeddedResource.BackingField.Example ??= EmbeddedResource.Read(EmbeddedResource.ResourceName.Example);
```

### Partial methods:
- `ReadEmbeddedResourceValue` - This method is called to allow the class
  to override how the value representing the content of the embedded
  resource is obtained. If the `backingField` parameter is null when this
  method ends, the default implementation will be used.
- `AlterEmbeddedResourceReturnValue` - This method is called after the
  text content has been read, and can be used to modify the text content
  before it is returned. Altering the returned value does not affect the
  value stored in the backing field.

```csharp
    [EmbeddedResourceProperties(".sql", "SqlQueries")]
    public static partial class SqlQuery
    {
        static partial void ReadEmbeddedResourceValue(
            ref string? backingField, 
            string resourceName, 
            string propertyName)
        {
            // This method is called before the default implementation.

            // The default implementation only reads the resource
            // if the backingField is null, so by setting it in this method,
            // the default implementation can be bypassed.

            // The backingField is a reference to a static field
            // for the property, and will be null for the first call,
            // but will retain the value for subsequent calls
            // for the same property.

            // Use custom logic to read the text content given the names
            // of the resource, and of the property.
            backingField ??= CustomReader(resourceName, propertyName);
        }

        static partial void AlterEmbeddedResourceReturnValue(
            ref string value,
            string resourceName,
            string propertyName);
        {
            // The value parameter is a reference to a variable that 
            // will be returned as the value of the property.
            
            // Implement custom logic to alter the value.
            value = CustomValueAlteringMethod(value, resourceName, propertyName);
        }
    }
```

This is an example of the code generated for a property, showing how the partial methods are called:

```csharp
/// <summary>Text value of the Embedded Resource: Example.txt</summary>
/// <value>
/// <code>
/// This is the content of the Example.txt file.
/// Only the first few lines are shown here.
/// </code>
/// </value>
/// <remarks>
/// The value is read from the embedded resource on first access.
/// </remarks>
public static string Example
{
    get
    {
        ReadEmbeddedResourceValue(ref EmbeddedResource.BackingField.Example, EmbeddedResource.ResourceName.Example, "Example");
        var value = EmbeddedResource.BackingField.Example ??= EmbeddedResource.Read(EmbeddedResource.ResourceName.Example);
        AlterEmbeddedResourceReturnValue(ref value, EmbeddedResource.ResourceName.Example, "Example");
        return value;
    }
}
```

## Doc-comment Cache
Any change in the IDE might result in the need for a change in the generated sources. To make this efficient,
incremental source generators set up pipelines which extract just the information that they need,
and pipelines only continue through to generating sources when there are changes to the collected information.

In order to include doc-comments on properties, the embedded resource file needs to be read.

Since embedded resources are different from source code, the source generators cannot tell whether the
contents of the embedded resource file have changed, without reading the file.

In order to avoid re-reading all the embedded resource files every time you make an edit in your source code,
the source generator only reads the matching embedded resource files once,
and caches the doc-comments for each matching file.

The cache can be refreshed for the embedded resource files of an attribute,
by temporarily enabling the `RegenerateDocCommentsWhileEditing` property on the attribute.

```csharp
[EmbeddedResourceProperties(".sql", "SqlQueries", RegenerateDocCommentsWhileEditing = true)]
public static partial class SqlQuery;
``` 

Editing the attribute to set that property to true, should trigger the update of the doc-comments
on the properties in that class, and then the property can be removed again.

## Diagnostics
The source generator traces its behaviour using https://github.com/datacute/LightweightTracing

The trace log can be appended to the generated source files by setting the `DiagnosticTraceLog`
property to true.

```csharp
[EmbeddedResourceProperties(".sql", "SqlQueries", DiagnosticTraceLog = true)]
public static partial class SqlQuery;
``` 

## Thanks

Thanks to Andrew Lock for his Series: [Creating a source generator](https://andrewlock.net/series/creating-a-source-generator/).

## Future Enhancements
- [ ] Add an option to leave out the Read method.
  - It is now included in the generated code for each class,
    but an implementation of the `ReadEmbeddedResourceValue` partial
    method might make it unnecessary.
- [ ] Support for alternative text encodings.
  - Overriding `ReadEmbeddedResourceValue` is a technique that can be
    used to read the text content of the embedded resource with a
    different encoding, but the doc-comment will still be generated
    expecting UTF-8.
- [ ] Support for specifying the number of lines to include in the
  doc-comment (including zero to exclude the code section).
  - This is currently set to 10 lines.
- [ ] Support generating text formatting methods.
  - Call `CompositeFormat.Parse` on the loaded text, and additionally count
    the number of format items, and generate a method that takes the
    same number of arguments.
  - Using a resx file is probably a better fit that adding this feature.
- [ ] Support generating `ReadOnlySpan<byte>` properties instead of `string`
  - The decoding from utf-8 may not be needed.
- [ ] Make use of the `field` keyword when C# 14 or above is used.
  - See https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14#the-field-keyword
