﻿//HintName: Query.g.cs
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Datacute.EmbeddedResourcePropertyGenerator.
// </auto-generated>
//------------------------------------------------------------------------------

#nullable enable

file static class ReadingMethods
{
    public static string ReadEmbeddedResource(string resourceName)
    {
        return global::Datacute.EmbeddedResourcePropertyGenerator.EmbeddedResourceReader.Read(typeof(Query), resourceName);
    }
}
file static class BackingFields
{
    public static string? Example;
}
file static class ResourceNames
{
    public const string Example = "EmbeddedResourcePropertyGenerator.Tests.Queries.example.sql";
}
/// <summary>
/// This class's properties are generated from project files meeting the criteria:
/// <list type="bullet">
/// <item>
/// <description>they are both an <c>EmbeddedResource</c> and an <c>AdditionalFile</c></description>
/// </item>
/// <item>
/// <description>they are in the project folder <c>Queries</c></description>
/// </item>
/// <item>
/// <description>they have the extension <c>.sql</c></description>
/// </item>
/// </list>
/// </summary>
public static partial class Query
{
    static partial void ReadEmbeddedResourceValue(ref string? backingField, string resourceName, string propertyName);
    static partial void AlterEmbeddedResourceReturnValue(ref string value, string resourceName, string propertyName);

    /// <summary>Text value of the Embedded Resource: example.sql</summary>
    /// <value>
    /// <code>
    /// Example sql content
    /// </code>
    /// </value>
    /// <remarks>
    /// The value is read from the embedded resource on first access.
    /// </remarks>
    public static string Example
    {
        get
        {
            ReadEmbeddedResourceValue(ref BackingFields.Example, ResourceNames.Example, "Example");
            var value = BackingFields.Example ??= ReadingMethods.ReadEmbeddedResource(ResourceNames.Example);
            AlterEmbeddedResourceReturnValue(ref value, ResourceNames.Example, "Example");
            return value;
        }
    }
}
