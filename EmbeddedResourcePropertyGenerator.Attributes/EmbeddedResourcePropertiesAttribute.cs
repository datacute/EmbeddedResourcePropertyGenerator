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
/// Source generators only have access to <c>AdditionalFiles</c>.
/// All <c>EmbeddedResource</c> files can be automatically included as <c>AdditionalFiles</c>
/// by including the following line in the project file:
/// <code>
/// &lt;ItemGroup&gt;
///     &lt;AdditionalFileItemNames&gt;$(AdditionalFileItemNames);EmbeddedResource&lt;/AdditionalFileItemNames&gt;
/// &lt;/ItemGroup&gt;
/// </code>
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