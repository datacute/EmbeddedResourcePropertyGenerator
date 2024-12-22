namespace Datacute.EmbeddedResourcePropertyGenerator;

public readonly struct EmbeddedResource(string path, string? docCommentCode)
{
    public string Path { get; } = path;
    public string? DocCommentCode { get; } = docCommentCode;
}