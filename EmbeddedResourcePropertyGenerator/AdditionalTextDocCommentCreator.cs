using System.Text;
using Microsoft.CodeAnalysis;

namespace Datacute.EmbeddedResourcePropertyGenerator;

public static class AdditionalTextDocCommentCreator
{
    public static string? GenerateDocCommentCode(AdditionalText additionalText, CancellationToken ct)
    {
        var sourceText = additionalText.GetText(ct);

        if (sourceText is null)
        {
            return null;
        }

        var sb = new StringBuilder();
        var textLineCollection = sourceText.Lines;
        var lineCount = textLineCollection.Count;
        var outputLines = 0;
        foreach (var textLine in textLineCollection)
        {
            outputLines++;
            if (outputLines > 10 && lineCount > outputLines + 1)
            {
                var moreLines = $"... {lineCount - outputLines} more lines";
                sb.AppendLine()
                    .Append("    /// ").Append(moreLines);
                break;
            }
            var textString = textLine.ToString();
            var escapedLine = EscapeStringForDocComments(textString);
            sb.AppendLine()
                .Append("    /// ").Append(escapedLine);
        }

        return sb.ToString();
    }
    
    private static string EscapeStringForDocComments(string input) =>
        input.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");

}