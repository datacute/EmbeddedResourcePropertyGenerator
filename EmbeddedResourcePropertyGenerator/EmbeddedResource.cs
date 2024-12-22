namespace Datacute.EmbeddedResourcePropertyGenerator;

public readonly record struct EmbeddedResource(string Path, string? DocCommentCode)
{
    public string Path { get; } = Path;
    public string? DocCommentCode { get; } = DocCommentCode;
}