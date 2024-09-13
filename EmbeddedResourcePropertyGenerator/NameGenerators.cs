using System.Globalization;
using System.Text;

namespace Datacute.EmbeddedResourcePropertyGenerator
{
    internal static class NameGenerators
    {
        public static string GetHintName(this string typeDisplayName) =>
            $"{typeDisplayName.Replace('<', '_').Replace('>', '_')}.g.cs";

        public static string GetPropertyName(this string resourceFilePath, string enclosingTypeName)
        {
            var propertyName = ConvertToPropertyName(Path.GetFileNameWithoutExtension(resourceFilePath));
            if (propertyName.Length == 0 || enclosingTypeName.Equals(propertyName))
            {
                propertyName = ConvertToPropertyName(Path.GetFileName(resourceFilePath));
            }
            return propertyName;
        }

        private static readonly Dictionary<char, string> CharacterNames = new Dictionary<char, string>
        {
            { '.', "dot" },
            { '-', "minus" },
            { '+', "plus" },
            { '*', "times" },
            { '/', "slash" },
            { '%', "pct" },
            { '<', "lt" },
            { '>', "gt" },
            { '=', "eq" },
            { '&', "amp" },
            { '|', "pipe" },
            { '^', "hat" },
            { '!', "excl" },
            { '?', "quest" },
            { ':', "colon" },
            { ',', "comma" },
            { ';', "semi" },
            { '~', "tilde" },
            { '`', "grave" },
            { '@', "at" },
            { '#', "hash" },
            { '$', "dollar" },
            { '\\', "backslash" },
            { '\'', "apos" },
            { '"', "quot" },
            { '[', "start" }, // lsqb lbrack
            { ']', "end" },
            { '{', "begin" }, // lcub lbrace
            { '}', "finish" },
            { '(', "open" },  // lpar lparen
            { ')', "close" },
            { ' ', "space" },
            { '\t', "tab" },
            { '\r', "CR" },
            { '\n', "LF" }
        };

        private static string ConvertToPropertyName(string filename)
        {
            var validName = new StringBuilder();
            var previousCharacterEscaped = false;

            // Iterate through each character in the filename
            for (var i = 0; i < filename.Length; i++)
            {
                // Check if the character is a letter or a digit
                // Replace invalid characters with underscores
                var c = filename[i];
                var isValid = ValidCharacterForProperty(c);
                if (isValid)
                {
                    validName.Append(c);
                    previousCharacterEscaped = false;
                }
                else if (CharacterNames.TryGetValue(c, out var name))
                {
                    if (!previousCharacterEscaped)
                    {
                        validName.Append('_');
                    }
                    validName.Append(name);
                    validName.Append('_');
                    previousCharacterEscaped = true;
                }
                else
                {
                    if (!previousCharacterEscaped)
                    {
                        validName.Append('_');
                    }
                    validName.Append($"u{char.ConvertToUtf32(filename, i):X}");
                    validName.Append('_');
                    previousCharacterEscaped = true;
                }

                if (char.IsHighSurrogate(c))
                {
                    i += 1;
                }
            }

            // Handle leading digits
            if (validName.Length > 0 && !ValidFirstCharacterForProperty(validName))
            {
                validName.Insert(0, '_');
            }

            // Convert to CamelCase
            if (validName.Length > 0)
            {
                validName[0] = char.ToUpper(validName[0]);
            }

            return validName.ToString();
        }

        private static bool ValidCharacterForProperty(char c)
        {
            switch (CharUnicodeInfo.GetUnicodeCategory(c))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.Format:
                    return true;
                default:
                    return false;
            }
        }

        private static bool ValidFirstCharacterForProperty(StringBuilder validName)
        {
            var c = validName[0];
            return c == '_' || char.IsLetter(c);
        }

        public static string GetFileName(this string resourceFilePath) => Path.GetFileName(resourceFilePath);

        public static string GetEmbeddedResourceName(this string resourceFilePath, string projectDir, string defaultNamespace)
        {
            // Normalize the file path by replacing directory separators with dots
            // and removing the project root path if it's a prefix of the file path.
            var relativePath = resourceFilePath.StartsWith(projectDir)
                ? resourceFilePath.Substring(projectDir.Length)
                : resourceFilePath;
            var normalizedPath = relativePath
                .Replace(Path.DirectorySeparatorChar, '.')
                .Replace(Path.AltDirectorySeparatorChar, '.');

            // Prepend the default namespace of the assembly
            return $"{defaultNamespace}.{normalizedPath}";
        }
    }
}