using Datacute.EmbeddedResourcePropertyGenerator;
// ReSharper disable UnusedParameterInPartialMethod

Console.WriteLine("Each example to follow outputs the property value twice");
Console.WriteLine("to make it easier to understand the behaviour through debugging.");
Console.WriteLine();

Console.WriteLine("Example of reading an embedded resource via a property:");
Console.WriteLine("First call, the embedded resource will be read and the backing field set:");
Console.WriteLine(SqlQuery.SelectAll);
Console.WriteLine();
Console.WriteLine("Second call, the backing field will already be set so the embedded resource");
Console.WriteLine("does not need to be read:");
Console.WriteLine(SqlQuery.SelectAll);
Console.WriteLine();

Console.WriteLine("Example of altering an embedded resource value before returning it:");
Console.WriteLine("First call, the backing field value will be copied, converted to lowercase,");
Console.WriteLine("and returned:");
Console.WriteLine(SqlQueryLowercase.SelectAll);
Console.WriteLine();
Console.WriteLine("Second call, the backing field value hasn't changed, it will be copied again");
Console.WriteLine("converted to lowercase again, and returned:");
Console.WriteLine(SqlQueryLowercase.SelectAll);
Console.WriteLine();

Console.WriteLine("Example of overriding embedded resource value:");
Console.WriteLine("First call, the backing field value will be set:");
Console.WriteLine(SqlQueryOverrides.SelectAll);
Console.WriteLine();
Console.WriteLine("Second call, the backing field already has a value:");
Console.WriteLine(SqlQueryOverrides.SelectAll);
Console.WriteLine();


[EmbeddedResourceProperties(".sql", "SqlQueries")]
static partial class SqlQuery;


[EmbeddedResourceProperties(".sql", "SqlQueries")]
static partial class SqlQueryLowercase
{
    static partial void AlterEmbeddedResourceReturnValue(
        ref string value,
        string resourceName,
        string propertyName)
        => value = value.ToLower(); // note the second time, the value on input is still the original value
}


[EmbeddedResourceProperties(".sql", "SqlQueries")]
static partial class SqlQueryOverrides
{
    static partial void ReadEmbeddedResourceValue(
        ref string? backingField,
        string resourceName,
        string propertyName)
    {
        // note the second time, the backingField is not null
        if (backingField is null && resourceName == "SelectAll")
        {
            backingField = "SELECT * FROM NewCustomersView;";
        }
    }
}