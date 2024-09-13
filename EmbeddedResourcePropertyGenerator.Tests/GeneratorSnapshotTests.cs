using Microsoft.CodeAnalysis;

namespace EmbeddedResourcePropertyGenerator.Tests;

public class GeneratorSnapshotTests
{
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
        return TestHelper.Verify(source);
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
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\example.txt", 
                "Example text content"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\example2.file", 
                "Example text content with the wrong extension - should not be included"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\WrongFolder\example3.txt", 
                "Example text content in the wrong folder - should not be included")
        };

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, additionalTexts);
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
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\example.sql",
                "Example sql content"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\example2.txt",
                "Example text content with the wrong extension - should not be included"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Query\example3.sql",
                "Example sql content in the wrong folder (but matching class name) - should not be included")
        };

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, additionalTexts);
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
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\1example(1).txt", 
                "Property names cannot start with a number nor contain special characters"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\1example<1>.txt", 
                "Replacing special characters with underscores can cause name collisions"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\test.txt", 
                "Property names are camel-cased, so this should be Test"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\Test.txt", 
                "Upper-casing the first letter can cause name collisions"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\Queries.txt", 
                "Member names cannot be the same as their enclosing type"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\Queries_txt.txt", 
                "Using the full filename can cause name collisions"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\.txt", 
                "The filename without extension is blank, which is not a valid property name"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\_txt.txt", 
                "Using the full filename can cause name collisions"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\😂.txt", 
                "Emoji are not valid in property names"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\1(@#$)[.,] <:'|>.txt", 
                "special characters in file names"),
            new InMemoryAdditionalText(
                @"E:\EmbeddedResourcePropertyGenerator.Tests\Queries\&amp;.txt", 
                "special characters in file names")
        };

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, additionalTexts);
    }
}