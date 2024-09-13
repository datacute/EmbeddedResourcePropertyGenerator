using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace EmbeddedResourcePropertyGenerator.Tests;

public class InMemoryAdditionalText : AdditionalText
{
    private readonly string _path;
    private readonly SourceText _text;

    public InMemoryAdditionalText(string path, string content)
    {
        _path = path;
        _text = SourceText.From(
            new MemoryStream(Encoding.UTF8.GetBytes(content)), 
            Encoding.UTF8, 
            SourceHashAlgorithm.Sha1, 
            false, 
            true);
    }

    public override string Path => _path;

    public override SourceText GetText(CancellationToken cancellationToken = default)
    {
        return _text;
    }
}