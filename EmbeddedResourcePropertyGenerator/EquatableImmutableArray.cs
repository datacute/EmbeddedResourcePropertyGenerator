using System.Collections;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Datacute.EmbeddedResourcePropertyGenerator;

public sealed class EquatableImmutableArray<T> : IEquatable<EquatableImmutableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    public static EquatableImmutableArray<T> Empty { get; } = new(ImmutableArray<T>.Empty);


    // Static factory method with singleton handling
    public static EquatableImmutableArray<T> Create(ImmutableArray<T> values)
    {
        if (values.IsEmpty)
            return Empty;
            
        return new EquatableImmutableArray<T>(values);
    }
    
    private readonly ImmutableArray<T> _values;
    private readonly int _hashCode;
    private readonly int _length;
    public T this[int index] => _values[index];
    public int Count => _length;

    private EquatableImmutableArray(ImmutableArray<T> values)
    {
        _values = values;
        _length = _values.Length;
        
        // Calculate hash code once during construction
        // The source generation pipelines compare these a lot
        // so being able to quickly tell when they are different
        // is important.
        var comparer = EqualityComparer<T>.Default;
        var hash = 0;
        for (var index = 0; index < _length; index++)
        {
            var value = _values[index];
            hash = HashHelpers_Combine(hash, value is null ? 0 : comparer.GetHashCode(value));
        }

        _hashCode = hash;
    }
    
    public bool Equals(EquatableImmutableArray<T>? other)
    {
        // Fast reference equality check
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;

        // If hash codes are different, arrays can't be equal
        if (_hashCode != other._hashCode)
            return false;

        // Compare array lengths
        if (_length != other._length) return false;

        // If both are empty, they're equal
        if (_length == 0) return true;

        // Element-by-element comparison
        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < _length; i++)
        {
            if (!comparer.Equals(_values[i], other._values[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableImmutableArray<T> other && Equals(other);

    public override int GetHashCode() => _hashCode;

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
        return EquatableImmutableArray<T>.Create(builder.MoveToImmutable());
    }
    public static EquatableImmutableArray<T> ToEquatableImmutableArray<TSource,T>(this EquatableImmutableArray<TSource> values, Func<TSource,T> selector) where TSource : IEquatable<TSource> where T : IEquatable<T>
    {
        var builder = ImmutableArray.CreateBuilder<T>(values.Count);
        foreach (TSource value in values)
        {
            builder.Add(selector(value));
        }
        return EquatableImmutableArray<T>.Create(builder.MoveToImmutable());
    }
    public static EquatableImmutableArray<T> ToEquatableImmutableArray<T>(this IEnumerable<T> values) where T : IEquatable<T> => EquatableImmutableArray<T>.Create(values.ToImmutableArray());

    public static EquatableImmutableArray<T> ToEquatableImmutableArray<T>(this ImmutableArray<T> values) where T : IEquatable<T> => EquatableImmutableArray<T>.Create(values);

    public static IncrementalValuesProvider<(TLeft Left, EquatableImmutableArray<TRight> Right)> CombineEquatable<TLeft, TRight>(
        this IncrementalValuesProvider<TLeft> provider1, 
        IncrementalValuesProvider<TRight> provider2) 
        where TRight : IEquatable<TRight>
        => provider1.Combine(
            provider2.Collect()
                .Select<ImmutableArray<TRight>,EquatableImmutableArray<TRight>>(
                    (array, _) => EquatableImmutableArray<TRight>.Create(array)));
        
    public static IncrementalValueProvider<(TLeft Left, EquatableImmutableArray<TRight> Right)> CombineEquatable<TLeft, TRight>(
        this IncrementalValueProvider<TLeft> provider1, 
        IncrementalValuesProvider<TRight> provider2) 
        where TRight : IEquatable<TRight>
        => provider1.Combine(
            provider2.Collect()
                .Select<ImmutableArray<TRight>,EquatableImmutableArray<TRight>>(
                    (array, _) => EquatableImmutableArray<TRight>.Create(array)));
        
}
