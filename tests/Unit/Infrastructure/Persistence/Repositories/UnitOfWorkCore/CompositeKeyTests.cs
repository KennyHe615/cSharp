using Infrastructure.Persistence.Repositories.UnitOfWorkCore;

using Xunit;


namespace tests.Unit.Infrastructure.Persistence.Repositories.UnitOfWorkCore;

public sealed class CompositeKeyTests
{
    [Fact]
    public void Equals_SameValuesSameOrder_ReturnsTrue_AndSameHashCode()
    {
        CompositeKey left = new CompositeKey([1, "A", null]);
        CompositeKey right = new CompositeKey([1, "A", null]);

        Assert.True(left.Equals(right));
        Assert.True(right.Equals(left));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentOrder_ReturnsFalse()
    {
        CompositeKey left = new CompositeKey([1, "A"]);
        CompositeKey right = new CompositeKey(["A", 1]);

        Assert.False(left.Equals(right));
        Assert.False(left.Equals((object)right));
    }

    [Fact]
    public void Equals_DifferentLength_ReturnsFalse()
    {
        CompositeKey left = new CompositeKey([1, "A"]);
        CompositeKey right = new CompositeKey([1, "A", 3]);

        Assert.False(left.Equals(right));
    }

    [Fact]
    public void Equals_WithNullComponents_WorksCorrectly()
    {
        CompositeKey left = new CompositeKey([null, "A", null]);
        CompositeKey right = new CompositeKey([null, "A", null]);
        CompositeKey different = new CompositeKey([null, "B", null]);

        Assert.True(left.Equals(right));
        Assert.False(left.Equals(different));
    }

    [Fact]
    public void Equals_Object_NonCompositeKey_ReturnsFalse()
    {
        CompositeKey key = new CompositeKey([1, "A"]);

        Assert.False(key.Equals(new object()));
        Assert.False(key.Equals(null));
    }

    [Fact]
    public void ToString_FormatsValuesAndNulls()
    {
        CompositeKey key = new CompositeKey([1, "A", null]);

        Assert.Equal("[1, A, null]", key.ToString());
    }
}
