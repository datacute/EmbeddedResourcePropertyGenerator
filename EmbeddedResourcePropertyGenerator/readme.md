# Embedded Resource Property Generator
This project generates properties for embedded resources in a project.

In a similar way to how the resx source generator adds properties for
strings or objects in a resx file, this project adds properties
where the embedded resources are a directory of files.

By adding the `EmbeddedResourceProperties` attribute to a partial class,
and specifying the filename extension and folder path to search,
the source generator will add a property to the class
for each matching embedded resource file. Accessing the property will
return the text content of the embedded resource file.

## Why not just use a resx file?
The use case for this project is when you have a large number
of text files, such as SQL queries, that you want to embed in your project,
and you wish to develop these files with an external editor that supports
syntax highlighting, execution, debugging, and other features.

While resx files do support including files, doing so requires either 
the use of another custom editor, or manipulating the xml of the resx 
file directly.

## What's wrong with just using Assembly.GetManifestResourceStream?
The purpose of providing properties to access the text, is to aid the developer
by generating doc-comments for the properties, showing the first few lines of the file.

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
4. Add text files to your project, in that directory, and set their Build Action to `EmbeddedResource`.
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
7. Put the attribute `[EmbeddedResourcePropertyGenerator]` on the class.
8. Specify the extension and folder path to search for embedded resources.
9. Use the properties generated on the partial class.
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

## Non-text File Types
This project expects text files so that it can generate properties that are strings.
It currently expects UTF-8 encoded files.

If you need to embed other types of files, use the `Assembly.GetManifestResourceStream` method directly.

## Extending the Behaviour
The implementation supports including two ***partial methods*** that can be implemented in the same partial class as the generated properties.
- `ReadEmbeddedResourceValue` - This method is called to read the text content of the embedded resource.
- `AlterEmbeddedResourceReturnValue` - This method is called after the text content has been read, and can be used to modify the text content before it is returned.

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

## Future Enhancements
- [ ] Support for alternative text encodings.
  - Overriding `ReadEmbeddedResourceValue` is a technique that can be used to 
    read the text content of the embedded resource with a different encoding,
    but the doc-comment will still be generated expecting UTF-8.
- [ ] Support for Specifying the number of lines to include in the doc-comment (including zero to exclude the code section).
