using System;

namespace Datacute.EmbeddedResourcePropertyGenerator;

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