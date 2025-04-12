using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Datacute.EmbeddedResourcePropertyGenerator;

public sealed class EquatableImmutableArray<T> : IEquatable<EquatableImmutableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    public static EquatableImmutableArray<T> Empty { get; } = new(ImmutableArray<T>.Empty);

    private readonly ImmutableArray<T> _values;
    public T this[int index] => _values[index];
    public int Count => _values.Length;

    public EquatableImmutableArray(ImmutableArray<T> values) => _values = values;
    public bool Equals(EquatableImmutableArray<T>? other) => other != null && _values.SequenceEqual(other._values);
    public override bool Equals(object? obj) => obj is EquatableImmutableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        int hash = 0;
        foreach (T value in _values)
        {
            hash = HashHelpers_Combine(hash, value is null ? 0 : value.GetHashCode());
        }

        return hash;
    }

    private static int HashHelpers_Combine(int h1, int h2)
    {
        // RyuJIT optimizes this to use the ROL instruction
        // Related GitHub pull request: https://github.com/dotnet/coreclr/pull/1830
        uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
        return ((int)rol5 + h1) ^ h2;
    }
    
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();
}

public static class EquatableImmutableArrayExtensions
{
    public static EquatableImmutableArray<T> ToEquatableImmutableArray<TSource,T>(this ImmutableArray<TSource> values, Func<TSource,T> selector) where T : IEquatable<T>
    {
        var builder = ImmutableArray.CreateBuilder<T>(values.Length);
        foreach (TSource value in values)
        {
            builder.Add(selector(value));
        }
        return new(builder.MoveToImmutable());
    }
    public static EquatableImmutableArray<T> ToEquatableImmutableArray<TSource,T>(this EquatableImmutableArray<TSource> values, Func<TSource,T> selector) where TSource : IEquatable<TSource> where T : IEquatable<T>
    {
        var builder = ImmutableArray.CreateBuilder<T>(values.Count);
        foreach (TSource value in values)
        {
            builder.Add(selector(value));
        }
        return new(builder.MoveToImmutable());
    }
    public static EquatableImmutableArray<T> ToEquatableImmutableArray<T>(this IEnumerable<T> values) where T : IEquatable<T> => new(values.ToImmutableArray());
    public static EquatableImmutableArray<T> ToEquatableImmutableArray<T>(this ImmutableArray<T> values) where T : IEquatable<T> => new(values);

    public static IncrementalValuesProvider<(TLeft Left, EquatableImmutableArray<TRight> Right)> CombineEquatable<TLeft, TRight>(
        this IncrementalValuesProvider<TLeft> provider1, 
        IncrementalValuesProvider<TRight> provider2) 
        where TRight : IEquatable<TRight>
        => provider1.Combine(
            provider2.Collect()
                .Select<ImmutableArray<TRight>,EquatableImmutableArray<TRight>>(
                    (array, _) => new EquatableImmutableArray<TRight>(array)));
        
    public static IncrementalValueProvider<(TLeft Left, EquatableImmutableArray<TRight> Right)> CombineEquatable<TLeft, TRight>(
        this IncrementalValueProvider<TLeft> provider1, 
        IncrementalValuesProvider<TRight> provider2) 
        where TRight : IEquatable<TRight>
        => provider1.Combine(
            provider2.Collect()
                .Select<ImmutableArray<TRight>,EquatableImmutableArray<TRight>>(
                    (array, _) => new EquatableImmutableArray<TRight>(array)));
        
}
