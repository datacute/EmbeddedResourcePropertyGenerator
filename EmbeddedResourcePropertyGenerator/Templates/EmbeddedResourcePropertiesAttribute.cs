using System;

// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    /// <summary>
    /// Use a source generator to add properties to this class for each embedded resource file 
    /// with a filename matching the given <see cref="Extension">Extension</see>
    /// found in the given <see cref="Path">Path</see>.
    /// <para>
    /// If the path starts with "/" it is relative to the project root,
    /// otherwise it is relative to the folder containing the class with this attribute.
    /// If the path is not specified, the class name is used.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated code includes a private nested class <c>EmbeddedResource</c> containing:
    /// <list type="table">
    /// <listheader><term>Method or Class</term><description>Purpose</description></listheader>
    /// <item><term><c>Read(string resourceName)</c></term><description>Method for reading embedded resources</description></item>
    /// <item><term><c>BackingField</c></term><description>Nested class caching the property values (omitted on C# 14 and later, which uses the <c>field</c> keyword directly)</description></item>
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
    /// public static string Example =&gt;
    ///         EmbeddedResource.BackingField.Example ??= EmbeddedResource.Read(EmbeddedResource.ResourceName.Example);
    /// </code>
    /// On C# 14 and later, the <c>field</c> keyword is used instead:
    /// <code>
    /// public static string Example =&gt;
    ///         field ??= EmbeddedResource.Read(EmbeddedResource.ResourceName.Example);
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
    /// On C# 14 and later, <c>EmbeddedResource.BackingField.Example</c> is replaced with the <c>field</c> keyword.
    /// </para>
    /// </remarks>
    [System.Diagnostics.Conditional("DATACUTE_EMBEDDEDRESOURCEPROPERTIES_USAGES")]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal sealed class EmbeddedResourcePropertiesAttribute : Attribute
    {
        /// <value>The filename extension of the embedded resource files
        /// to include as properties, defaulting to ".txt".</value>
        public string Extension { get; set; }

        /// <value>The path of the directory containing embedded resource files
        /// to include as properties.</value>
        /// <remarks>
        /// If the path starts with "/" it is treated as relative to the project root,
        /// otherwise it is relative to the folder containing the class with this attribute.
        /// If the path is not specified, the class name is used.
        /// </remarks>
        public string Path { get; set; }

        /// <value>
        /// Output a diagnostic trace log as a comment at the end of the generated files
        /// </value>
        /// <remarks>
        /// The log shows timestamps for each step of the Embedded Resource Property incremental source code generation process,
        /// </remarks>
        public bool DiagnosticTraceLog { get; set; }

        /// <summary>
        /// Use a source generator to add properties to this class for each embedded resource file 
        /// found in the <see cref="Path">path</see> with the folder name the same as this class,
        /// and where the file name's <see cref="Extension">extension</see> is ".txt"
        /// </summary>
        public EmbeddedResourcePropertiesAttribute() : this(".txt", null)
        {
        }

        /// <summary>
        /// Use a source generator to add properties to this class for each embedded resource file 
        /// found in the <see cref="Path">path</see> with the folder name the same as this class,
        /// and where the file name's <see cref="Extension">extension</see> matches the given extension.
        /// </summary>
        /// <param name="extension">The file name extension to include</param>
        public EmbeddedResourcePropertiesAttribute(string extension = ".txt") : this(extension, null)
        {
        }

        /// <summary>
        /// Use a source generator to add properties to this class for each embedded resource file 
        /// found in the specified <see cref="Path">path</see>,
        /// and where the file name's <see cref="Extension">extension</see> matches the given extension.
        /// </summary>
        /// <param name="extension">The file name extension to include</param>
        /// <param name="path">The folder path to include</param>
        public EmbeddedResourcePropertiesAttribute(string extension = ".txt", string path = null)
        {
            Extension = extension;
            Path = path;
        }
    }
}