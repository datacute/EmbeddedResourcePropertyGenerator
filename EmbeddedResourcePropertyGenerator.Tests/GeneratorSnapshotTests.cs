using Datacute.EmbeddedResourcePropertyGenerator;
using Microsoft.CodeAnalysis;

namespace EmbeddedResourcePropertyGenerator.Tests;

public class GeneratorSnapshotTests
{
    private static Task Verify(string source, List<AdditionalText>? additionalTexts = null)
    {
        return TestHelper.Verify<EmbeddedResourcePropertiesAttribute, Generator>(source, additionalTexts);
    }

    [Fact]
    public Task GeneratesEmbeddedResourcePropertiesWithNoProperties()
    {
        // The source code to test
        var source = /* language=c# */
            """
            using Datacute.EmbeddedResourcePropertyGenerator;

            [EmbeddedResourceProperties]
            public static partial class Queries;
            """;

        // Pass the source code to our helper and snapshot test the output
        return Verify(source);
    }

    [Fact]
    public Task GeneratesEmbeddedResourcePropertiesCorrectly()
    {
        // The source code to test
        var source = /* language=c# */
            """
            using Datacute.EmbeddedResourcePropertyGenerator;

            [EmbeddedResourceProperties]
            public static partial class Queries;
            """;

        // Create a list to hold all additional texts
        var additionalTexts = new List<AdditionalText>
        {
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\example.txt", 
                "Example text content"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\example2.file", 
                "Example text content with the wrong extension - should not be included"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\WrongFolder\example3.txt", 
                "Example text content in the wrong folder - should not be included")
        };

        // Pass the source code to our helper and snapshot test the output
        return Verify(source, additionalTexts);
    }

    [Fact]
    public Task AlternateFolderSelectedCorrectly()
    {
        // The source code to test
        var source = /* language=c# */
            """
            using Datacute.EmbeddedResourcePropertyGenerator;

            [EmbeddedResourceProperties(".sql", "Queries")]
            public static partial class Query;
            """;

        // Create a list to hold all additional texts
        var additionalTexts = new List<AdditionalText>
        {
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\example.sql",
                "Example sql content"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\example2.txt",
                "Example text content with the wrong extension - should not be included"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Query\example3.sql",
                "Example sql content in the wrong folder (but matching class name) - should not be included")
        };

        // Pass the source code to our helper and snapshot test the output
        return Verify(source, additionalTexts);
    }

    [Fact]
    public Task GeneratesEmbeddedResourcePropertiesWithStrangeNamesCorrectly()
    {
        // The source code to test
        var source = /* language=c# */
            """
            using Datacute.EmbeddedResourcePropertyGenerator;

            [EmbeddedResourceProperties]
            public static partial class Queries;
            """;

        // Create a list to hold all additional texts
        var additionalTexts = new List<AdditionalText>
        {
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\1example(1).txt", 
                "Property names cannot start with a number nor contain special characters"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\1example<1>.txt", 
                "Replacing special characters with underscores can cause name collisions"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\test.txt", 
                "Property names are camel-cased, so this should be Test"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\Test.txt", 
                "Upper-casing the first letter can cause name collisions"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\Queries.txt", 
                "Member names cannot be the same as their enclosing type"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\Queries_txt.txt", 
                "Using the full filename can cause name collisions"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\.txt", 
                "The filename without extension is blank, which is not a valid property name"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\_txt.txt", 
                "Using the full filename can cause name collisions"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\😂.txt", 
                "Emoji are not valid in property names"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\1(@#$)[.,] <:'|>.txt", 
                "special characters in file names"),
            new InMemoryAdditionalText(
                $@"{TestHelper.TestPath}\Queries\&amp;.txt", 
                "special characters in file names")
        };

        // Pass the source code to our helper and snapshot test the output
        return Verify(source, additionalTexts);
    }
}