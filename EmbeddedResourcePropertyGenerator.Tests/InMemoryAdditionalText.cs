using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace EmbeddedResourcePropertyGenerator.Tests;

public class InMemoryAdditionalText(string path, string content) : AdditionalText
{
    public override string Path => path;

    public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(
        new MemoryStream(Encoding.UTF8.GetBytes(content)), 
        Encoding.UTF8, 
        SourceHashAlgorithm.Sha1, 
        false, 
        true);
}