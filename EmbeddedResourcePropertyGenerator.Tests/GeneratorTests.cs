﻿using Datacute.EmbeddedResourcePropertyGenerator;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;

namespace EmbeddedResourcePropertyGenerator.Tests;

public class GeneratorTests
{
    [Fact]
    public void CanGenerate()
    {
        const string input = /* language=c# */
            $$"""
              using Datacute.EmbeddedResourcePropertyGenerator;
              namespace {{TestHelper.TestNamespace}}
              {
                  [EmbeddedResourceProperties]
                  public static partial class Queries;
              }
              """;
        
        // Create a list to hold all additional texts
        var additionalTexts = new List<AdditionalText>
        {
            new InMemoryAdditionalText(
                TestHelper.TestPath("Queries/example.txt"),
                "Example text content"),
            new InMemoryAdditionalText(
                TestHelper.TestPath("Queries/example2.file"),
                "Example text content with the wrong extension - should not be included"),
            new InMemoryAdditionalText(
                TestHelper.TestPath("WrongFolder/example3.txt"),
                "Example text content in the wrong folder - should not be included")
        };
        
        const string expected = /* language=c# */
            $$"""
              //------------------------------------------------------------------------------
              // <auto-generated>
              //     This code was generated by the Datacute.EmbeddedResourcePropertyGenerator.
              // </auto-generated>
              //------------------------------------------------------------------------------

              #nullable enable

              namespace {{TestHelper.TestNamespace}};
              /// <summary>
              /// This class's properties are generated from project files meeting the criteria:
              /// <list type="bullet">
              /// <item>
              /// <description>they are both an <c>EmbeddedResource</c> and an <c>AdditionalFile</c></description>
              /// </item>
              /// <item>
              /// <description>they are in the project folder <c>Queries</c></description>
              /// </item>
              /// <item>
              /// <description>they have the extension <c>.txt</c></description>
              /// </item>
              /// </list>
              /// </summary>
              public static partial class Queries
              {
                  private static class EmbeddedResource
                  {
                      public static string Read(string resourceName)
                      {
                          var assembly = typeof(Queries).Assembly;
                          using var stream = assembly.GetManifestResourceStream(resourceName)!;
                          using var streamReader = new global::System.IO.StreamReader(stream, global::System.Text.Encoding.UTF8);
                          var resourceText = streamReader.ReadToEnd();
                          return resourceText;
                      }
                      public static class BackingField
                      {
                          public static string? Example;
                      }
                      public static class ResourceName
                      {
                          public const string Example = "{{TestHelper.TestNamespace}}.Queries.example.txt";
                      }
                  }
                  static partial void ReadEmbeddedResourceValue(ref string? backingField, string resourceName, string propertyName);
                  static partial void AlterEmbeddedResourceReturnValue(ref string value, string resourceName, string propertyName);
              
                  /// <summary>Text value of the Embedded Resource: example.txt</summary>
                  /// <value>
                  /// <code>
                  /// Example text content
                  /// </code>
                  /// </value>
                  /// <remarks>
                  /// The value is read from the embedded resource on first access.
                  /// </remarks>
                  public static string Example
                  {
                      get
                      {
                          ReadEmbeddedResourceValue(ref EmbeddedResource.BackingField.Example, EmbeddedResource.ResourceName.Example, "Example");
                          var value = EmbeddedResource.BackingField.Example ??= EmbeddedResource.Read(EmbeddedResource.ResourceName.Example);
                          AlterEmbeddedResourceReturnValue(ref value, EmbeddedResource.ResourceName.Example, "Example");
                          return value;
                      }
                  }
              }

              """;

        // run the generator, passing in the inputs and the tracking names
        var (diagnostics, output)
            = TestHelper.GetGeneratedOutput<EmbeddedResourcePropertiesAttribute, Generator, TrackingNames>(additionalTexts, input);

        // Assert the output
        using var s = new AssertionScope();
        diagnostics.Should().BeEmpty();
        output.LastOrDefault().Should().Be(expected);
    }
}