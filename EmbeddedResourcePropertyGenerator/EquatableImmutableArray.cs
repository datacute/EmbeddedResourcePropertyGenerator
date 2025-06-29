using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Datacute.EmbeddedResourcePropertyGenerator;

public sealed class EquatableImmutableArray<T> : IEquatable<EquatableImmutableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    public static EquatableImmutableArray<T> Empty { get; } = new(ImmutableArray<T>.Empty);

    // The source generation pipelines compare these a lot
    // so being able to quickly tell when they are different
    // is important.
    // We will use an instance cache to find when we can reuse
    // an existing object, massively speeding up the Equals call.
    #region Instance Cache

    // Thread-safe cache using dictionary of hash code -> list of arrays with that hash
    private static readonly ConcurrentDictionary<int, List<WeakReference<EquatableImmutableArray<T>>>> InstanceCache = new();

    // Static factory method with singleton handling
    public static EquatableImmutableArray<T> Create(ImmutableArray<T> values, CancellationToken cancellationToken = default)
    {
        if (values.IsEmpty)
            return Empty;
            
        // Calculate hash code for the values
        var hash = CalculateHashCode(values);

        // Try to find an existing instance with the same hash and values
        if (InstanceCache.TryGetValue(hash, out var list))
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (list) // Thread safety for the list
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (list[i].TryGetTarget(out var existing))
                    {
                        // Element-by-element comparison for arrays with the same hash
                        if (ValuesEqual(values, existing._values))
                            return existing;
                    }
                    else
                    {
                        // Remove dead references
                        list.RemoveAt(i);
                    }
                }
            }
        }

        // Create new instance and add to cache
        var result = new EquatableImmutableArray<T>(values, hash);
        
        InstanceCache.AddOrUpdate(hash, 
            _ => new List<WeakReference<EquatableImmutableArray<T>>> { new(result) },
            (_, existingList) => 
            {
                cancellationToken.ThrowIfCancellationRequested();
                lock (existingList)
                {
                    existingList.Add(new WeakReference<EquatableImmutableArray<T>>(result));
                }
                return existingList;
            });
        
        return result;
    }

    private static bool ValuesEqual(ImmutableArray<T> a, ImmutableArray<T> b)
    {
        // Identical arrays reference check
        if (a == b) return true;
        
        int length = a.Length;
        if (length != b.Length) return false;
        
        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < length; i++)
        {
            if (!comparer.Equals(a[i], b[i]))
                return false;
        }
        
        return true;
    }

    private static int CalculateHashCode(ImmutableArray<T> values)
    {
        var comparer = EqualityComparer<T>.Default;
        var hash = 0;
        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            hash = HashHelpers_Combine(hash, value is null ? 0 : comparer.GetHashCode(value));
        }
        return hash;
    }
    
    #endregion

    private readonly ImmutableArray<T> _values;
    private readonly int _hashCode;
    private readonly int _length;
    public T this[int index] => _values[index];
    public int Count => _length;

    private EquatableImmutableArray(ImmutableArray<T> values)
    {
        _values = values;
        _length = values.Length;
        _hashCode = CalculateHashCode(values);
    }
    
    private EquatableImmutableArray(ImmutableArray<T> values, int hashCode)
    {
        _values = values;
        _length = values.Length;
        _hashCode = hashCode;
    }
    
    public bool Equals(EquatableImmutableArray<T>? other)
    {
        // Fast reference equality check
        if (ReferenceEquals(this, other)) return true;

        if (other is null) return false;

        // If hash codes are different, arrays can't be equal
        if (_hashCode != other._hashCode)
            return false;

        // We're really unlikely to get here, as we're using an instance cache
        // so we've probably encountered a hash collision
        
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
    public static EquatableImmutableArray<T> ToEquatableImmutableArray<TSource,T>(this ImmutableArray<TSource> values, Func<TSource,T> selector, CancellationToken ct = default) where T : IEquatable<T>
    {
        var builder = ImmutableArray.CreateBuilder<T>(values.Length);
        foreach (TSource value in values)
        {
            builder.Add(selector(value));
        }
        return EquatableImmutableArray<T>.Create(builder.MoveToImmutable(), ct);
    }
    public static EquatableImmutableArray<T> ToEquatableImmutableArray<TSource,T>(this EquatableImmutableArray<TSource> values, Func<TSource,T> selector, CancellationToken ct = default) where TSource : IEquatable<TSource> where T : IEquatable<T>
    {
        var builder = ImmutableArray.CreateBuilder<T>(values.Count);
        foreach (TSource value in values)
        {
            builder.Add(selector(value));
        }
        return EquatableImmutableArray<T>.Create(builder.MoveToImmutable(), ct);
    }
    public static EquatableImmutableArray<T> ToEquatableImmutableArray<T>(this IEnumerable<T> values, CancellationToken ct = default) where T : IEquatable<T> => EquatableImmutableArray<T>.Create(values.ToImmutableArray(), ct);

    public static EquatableImmutableArray<T> ToEquatableImmutableArray<T>(this ImmutableArray<T> values, CancellationToken ct = default) where T : IEquatable<T> => EquatableImmutableArray<T>.Create(values, ct);

    public static IncrementalValuesProvider<(TLeft Left, EquatableImmutableArray<TRight> Right)> CombineEquatable<TLeft, TRight>(
        this IncrementalValuesProvider<TLeft> provider1, 
        IncrementalValuesProvider<TRight> provider2) 
        where TRight : IEquatable<TRight>
        => provider1.Combine(provider2.Collect().Select(EquatableImmutableArray<TRight>.Create));
        
    public static IncrementalValueProvider<(TLeft Left, EquatableImmutableArray<TRight> Right)> CombineEquatable<TLeft, TRight>(
        this IncrementalValueProvider<TLeft> provider1, 
        IncrementalValuesProvider<TRight> provider2) 
        where TRight : IEquatable<TRight>
        => provider1.Combine(provider2.Collect().Select(EquatableImmutableArray<TRight>.Create));
        
}
