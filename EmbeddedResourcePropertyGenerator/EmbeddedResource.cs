namespace Datacute.EmbeddedResourcePropertyGenerator;

public readonly struct EmbeddedResource
{
    public string Path { get; }
    public string? DocCommentCode { get; }

    public EmbeddedResource(string path, string? docCommentCode)
    {
        Path = path;
        DocCommentCode = docCommentCode;
    }
}