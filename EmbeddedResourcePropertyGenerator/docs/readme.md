# Embedded Resource Property Generator
This project generates properties for text file embedded resources in a
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
The purpose of providing properties to access the text, is to aid the developer
by generating doc-comments along with the properties, showing the start of the file.

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
    public static string GoodIndenting => _GoodIndenting ??= ReadEmbeddedResource("StyleGuide.GoodIndenting.cypher");
    private static string? _GoodIndenting;
```

The full names of the embedded resources are also generated, and no longer need to be supplied by the developer,
making it easy to move the location of the resource files to another directory,
without needing to find and fix all the references to the resource names.

## Usage
1. Add a reference to the `EmbeddedResourcePropertyGenerator` project.
2. Modify your project file to include the following:
```xml
    <PropertyGroup>
        <!-- Include all EmbeddedResource files as Additional Files for the source generators -->
        <AdditionalFileItemNames>$(AdditionalFileItemNames);EmbeddedResource</AdditionalFileItemNames>
    </PropertyGroup>
```
3. Add a directory to your project to group the files you want to embed.
5. Add text files to your project, in that directory, and set their Build Action to `EmbeddedResource`.
```
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
5. Add a partial class to your code.
6. Include a using statement to the namespace of the code generator.
7. Put the attribute `[EmbeddedResourcePropertyGenerator]` on the class
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

## Localisation and External Overrides
Localisation and External Overrides are not supported. If you need these features, consider using a resx file instead.

## Non-text file types
This project expects UTF-8 encoded text files, so that it can generate doc-comments.

If you need to embed other types of files, use the `Assembly.GetManifestResourceStream` method directly.

## Extending the Behaviour
