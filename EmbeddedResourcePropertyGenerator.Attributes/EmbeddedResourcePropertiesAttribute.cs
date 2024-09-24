using System;

namespace Datacute.EmbeddedResourcePropertyGenerator;

/// <summary>
/// Use a source generator to add properties to this class for each embedded resource file 
/// with a filename matching the given <see cref="Extension"/>
/// found in the given <see cref="Path"/>.
/// <para>
/// If the path starts with "/" it is relative to the project root,
/// otherwise it is relative to the folder containing the class with this attribute.
/// If the path is not specified, the class name is used.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// Source generators only have access to <c>AdditionalFiles</c>.
/// All <c>EmbeddedResource</c> files can be automatically included as <c>AdditionalFiles</c>
/// by including the following line in the project file:
/// <code>
/// &lt;ItemGroup&gt;
///     &lt;AdditionalFileItemNames&gt;$(AdditionalFileItemNames);EmbeddedResource&lt;/AdditionalFileItemNames&gt;
/// &lt;/ItemGroup&gt;
/// </code>
/// </para>
/// <para>
/// The generated code includes a private nested class <c>EmbeddedResource</c> containing:
/// <list type="table">
/// <listheader><term>Method or Class</term><description>Purpose</description></listheader>
/// <item><term><c>Read(string resourceName)</c></term><description>Method for reading embedded resources</description></item>
/// <item><term><c>BackingField</c></term><description>Nested class caching the property values</description></item>
/// <item><term><c>ResourceName</c></term><description>Nested class holding the resource names</description></item>
/// </list>
/// </para>
/// <para>
/// The generated code supports customising the behaviour of the property getters in two ways, with two partial methods
/// <c>ReadEmbeddedResourceValue</c> and <c>AlterEmbeddedResourceReturnValue</c>.
/// </para>
/// <para>
/// If the partial methods are not implemented, the code effectively reduces to:
/// <code>
/// public static string Example =>
///         EmbeddedResource.BackingField.Example ??= EmbeddedResource.Read(EmbeddedResource.ResourceName.Example);
/// </code>
/// </para>
/// <para>
/// Partial methods:
/// <list type="number">
/// <item>To allow customisation of how the backing field is read
/// (for example to check the file system for an override)
/// implement a partial method that will be called before the above, with the signature:
/// <code>
/// static partial void ReadEmbeddedResourceValue(ref string? backingField, string resourceName, string propertyName);
/// </code>
/// </item>
/// <item>
/// To allow customisation of the returned value, based on the backing field,
/// implement a partial method with the signature:
/// <code>
/// static partial void AlterEmbeddedResourceReturnValue(ref string value, string resourceName, string propertyName);
/// </code>
/// </item>
/// </list>
/// </para>
/// <para>
/// This is an example of the code generated for a property, showing how the partial methods are called:
/// <code>
/// public static string Example
/// {
///     get
///     {
///         ReadEmbeddedResourceValue(ref EmbeddedResource.BackingField.Example, EmbeddedResource.ResourceName.Example, "Example");
///         var value = EmbeddedResource.BackingField.Example ??= EmbeddedResource.Read(EmbeddedResource.ResourceName.Example);
///         AlterEmbeddedResourceReturnValue(ref value, EmbeddedResource.ResourceName.Example, "Example");
///         return value;
///     }
/// }
/// </code>
/// </para>
/// </remarks>
[System.Diagnostics.Conditional("DATACUTE_EMBEDDEDRESOURCEPROPERTIES_USAGES")]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class EmbeddedResourcePropertiesAttribute : Attribute
{
    /// <value>The filename extension of the embedded resource files
    /// to include as properties, defaulting to ".txt".</value>
    public string Extension { get; private set; }

    /// <value>The path of the directory of embedded resource files
    /// to include as properties.</value>
    /// <remarks>
    /// If the path starts with "/" it is treated as relative to the project root,
    /// otherwise it is relative to the folder containing the class with this attribute.
    /// If the path is not specified, the class name is used.
    /// </remarks>
    public string? Path { get; private set; }

    public EmbeddedResourcePropertiesAttribute(string extension = ".txt", string? path = null)
    {
        Extension = extension;
        Path = path;
    }
}