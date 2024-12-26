using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace EmbeddedResourcePropertyGenerator.Tests;

public class InMemoryAdditionalText : AdditionalText
{
    private readonly SourceText? _sourceText;
    public override string Path { get; }
    public TextSpan TextSpan => new(0, _sourceText?.Length ?? 0);
    public override SourceText? GetText(CancellationToken cancellationToken = default) => _sourceText;

    public InMemoryAdditionalText(string path, string content) : this(path, SourceText.From(
        new MemoryStream(Encoding.UTF8.GetBytes(content)), 
        Encoding.UTF8, 
        SourceHashAlgorithm.Sha1, 
        false, 
        true))
    {
    }

    private InMemoryAdditionalText(string path, SourceText? sourceText)
    {
        _sourceText = sourceText;
        Path = path;
    }
    
    public InMemoryAdditionalText Replace(TextSpan span, string newText) => new(Path, _sourceText?.Replace(span, newText));
}