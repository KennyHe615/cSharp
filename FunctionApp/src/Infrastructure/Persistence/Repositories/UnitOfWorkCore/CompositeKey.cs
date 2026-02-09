namespace Infrastructure.Persistence.Repositories.UnitOfWorkCore;

/// <summary>
/// Represents a composite primary key consisting of multiple property values.
/// Provides structural equality comparison based on the key component values.
/// </summary>
/// <remarks>
/// This class is used internally by the UnitOfWork to handle entities with composite primary keys.
/// It ensures that two composite keys are considered equal if all their component values are equal,
/// regardless of reference equality.
/// <para>
/// The hash code is computed once during construction for performance optimization,
/// making this class suitable for use in hash-based collections like <see cref="Dictionary{TKey,TValue}"/>
/// and <see cref="HashSet{T}"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is immutable and thread-safe after construction.
/// </para>
/// </remarks>
internal sealed class CompositeKey(object?[] values) : IEquatable<CompositeKey>
{
    private readonly object?[] _values = values;
    private readonly int _hashCode = CalculateHashCode(values);

    /// <summary>
    /// Determines whether the specified <see cref="CompositeKey"/> is equal to the current instance
    /// by comparing all component values for equality.
    /// </summary>
    /// <param name="other">The <see cref="CompositeKey"/> to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the specified <see cref="CompositeKey"/> has the same number of components
    /// and all corresponding component values are equal; otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(CompositeKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_values.Length != other._values.Length) return false;

        return !_values.Where((t, i) => !Equals(t, other._values[i])).Any();
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>
    /// <c>true</c> if the specified object is a <see cref="CompositeKey"/>
    /// and is equal to the current instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
    {
        return obj is CompositeKey other && Equals(other);
    }

    /// <summary>
    /// Returns the pre-computed hash code for this instance.
    /// </summary>
    /// <returns>
    /// A 32-bit signed integer hash code based on all component values.
    /// The hash code is computed once during construction for performance.
    /// </returns>
    /// <remarks>
    /// The hash code remains stable throughout the lifetime of the instance,
    /// making this class safe for use in hash-based collections.
    /// </remarks>
    public override int GetHashCode()
    {
        return _hashCode;
    }

    /// <summary>
    /// Returns a string representation of the composite key showing all component values.
    /// </summary>
    /// <returns>
    /// A string in the format "[value1, value2, ...]" where each value is converted to its string representation.
    /// Null values are represented as "null".
    /// </returns>
    public override string ToString()
    {
        return $"[{string.Join(", ", _values.Select(v => v?.ToString() ?? "null"))}]";
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Computes a stable hash code based on all component values using <see cref="HashCode"/> struct.
    /// </summary>
    /// <param name="values">The array of component values to hash.</param>
    /// <returns>A 32-bit signed integer hash code.</returns>
    /// <remarks>
    /// Uses the .NET <see cref="HashCode"/> struct which provides a high-quality hash function
    /// that combines multiple values while minimizing collisions.
    /// </remarks>
    private static int CalculateHashCode(object?[] values)
    {
        HashCode hash = new();

        foreach (object? value in values)
        {
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    #endregion
}
