using Datacute.IncrementalGeneratorExtensions;
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
            ct.ThrowIfCancellationRequested(109);

            // Truncation happens after 10 lines
            // but if there are only 11 lines,
            // we show the last line instead of a line saying that there is 1 more line.
            // (As a bonus - "x more lines" is always plural)
            const int maxLinesToShow = 10;
            if (outputLines >= maxLinesToShow && lineCount > maxLinesToShow + 1)
            {
                var moreLines = $"... {lineCount - outputLines} more lines";
                sb.AppendLine()
                    .Append("/// ").Append(moreLines);
                break;
            }

            var textString = textLine.ToString();
            var escapedLine = EscapeStringForDocComments(textString);
            sb.AppendLine()
                .Append("/// ").Append(escapedLine);
            outputLines++;
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