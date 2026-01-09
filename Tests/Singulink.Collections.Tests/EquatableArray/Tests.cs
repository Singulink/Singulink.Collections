using System.Collections.Immutable;
using System.Globalization;

namespace Singulink.Collections.Tests.EquatableArray;

using EquatableArray = Singulink.Collections.EquatableArray;

[PrefixTestClass]
public class Tests
{
    private static void ShouldBeSequenceEqual<T>(EquatableArray<T> array, ReadOnlySpan<T> expected)
    {
        array.Length.ShouldBe(expected.Length);
        array.IsEmpty.ShouldBe(expected.Length == 0);

        for (int i = 0; i < expected.Length; i++)
            array[i].ShouldBe(expected[i]);
    }

    [TestMethod]
    public void Empty_ReturnsEmptyArray()
    {
        var empty = EquatableArray<int>.Empty;

        ShouldBeSequenceEqual(empty, []);
    }

    [TestMethod]
    public void Constructor_WithDefaultImmutableArray_ReturnsEmptyArray()
    {
        ImmutableArray<int> defaultArray = default;
        var array = new EquatableArray<int>(defaultArray);

        ShouldBeSequenceEqual(array, []);
    }

    [TestMethod]
    public void Constructor_WithEmptyImmutableArray_CreatesEmptyArray()
    {
        var array = new EquatableArray<int>(ImmutableArray<int>.Empty);

        ShouldBeSequenceEqual(array, []);
    }

    [TestMethod]
    public void Constructor_WithItems_CreatesArrayWithItems()
    {
        var immutable = ImmutableArray.Create(1, 2, 3);
        var array = new EquatableArray<int>(immutable);

        ShouldBeSequenceEqual(array, [1, 2, 3]);
    }

    [TestMethod]
    public void Create_FromImmutableArray_Empty_ReturnsEmptyArray()
    {
        var result = EquatableArray.Create(ImmutableArray<int>.Empty);

        ShouldBeSequenceEqual(result, []);
    }

    [TestMethod]
    public void Create_FromImmutableArray_Default_ReturnsEmptyArray()
    {
        ImmutableArray<int> defaultArray = default;
        var result = EquatableArray.Create(defaultArray);

        ShouldBeSequenceEqual(result, []);
    }

    [TestMethod]
    public void Create_FromImmutableArray_SingleItem_CreatesCorrectArray()
    {
        var result = EquatableArray.Create(ImmutableArray.Create(42));

        ShouldBeSequenceEqual(result, [42]);
    }

    [TestMethod]
    public void Create_FromImmutableArray_MultipleItems_CreatesCorrectArray()
    {
        var result = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        ShouldBeSequenceEqual(result, [1, 2, 3]);
    }

    [TestMethod]
    public void Create_FromEnumerable_Empty_ReturnsEmptyArray()
    {
        var result = EquatableArray.Create(Enumerable.Empty<int>());

        ShouldBeSequenceEqual(result, []);
    }

    [TestMethod]
    public void Create_FromEnumerable_SingleItem_CreatesCorrectArray()
    {
        var result = EquatableArray.Create(new[] { 42 }.AsEnumerable());

        ShouldBeSequenceEqual(result, [42]);
    }

    [TestMethod]
    public void Create_FromEnumerable_MultipleItems_CreatesCorrectArray()
    {
        var result = EquatableArray.Create(new[] { 1, 2, 3 }.AsEnumerable());

        ShouldBeSequenceEqual(result, [1, 2, 3]);
    }

    [TestMethod]
    public void Create_FromImmutableArrayAsEnumerable_ReturnsEquivalent()
    {
        var immutable = ImmutableArray.Create(1, 2, 3);
        var result = EquatableArray.Create((IEnumerable<int>)immutable);

        ShouldBeSequenceEqual(result, [1, 2, 3]);
    }

    [TestMethod]
    public void Create_FromEquatableArrayAsEnumerable_ReturnsEqualArray()
    {
        var original = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var result = EquatableArray.Create((IEnumerable<int>)original);

        result.ShouldBe(original);
        ShouldBeSequenceEqual(result, [1, 2, 3]);
    }

    [TestMethod]
    public void Create_FromReadOnlySpan_Empty_ReturnsEmptyArray()
    {
        var result = EquatableArray.Create(ReadOnlySpan<int>.Empty);

        ShouldBeSequenceEqual(result, []);
    }

    [TestMethod]
    public void Create_FromReadOnlySpan_SingleItem_CreatesCorrectArray()
    {
        ReadOnlySpan<int> span = [42];
        var result = EquatableArray.Create(span);

        ShouldBeSequenceEqual(result, [42]);
    }

    [TestMethod]
    public void Create_FromReadOnlySpan_MultipleItems_CreatesCorrectArray()
    {
        ReadOnlySpan<int> span = [1, 2, 3];
        var result = EquatableArray.Create(span);

        ShouldBeSequenceEqual(result, [1, 2, 3]);
    }

    [TestMethod]
    public void ToEquatableArray_FromImmutableArray_CreatesEquivalent()
    {
        var immutable = ImmutableArray.Create(1, 2, 3);
        var result = immutable.ToEquatableArray();

        ShouldBeSequenceEqual(result, [1, 2, 3]);
    }

    [TestMethod]
    public void ToEquatableArray_FromEnumerable_CreatesEquivalent()
    {
        var result = new[] { 1, 2, 3 }.AsEnumerable().ToEquatableArray();

        ShouldBeSequenceEqual(result, [1, 2, 3]);
    }

    [TestMethod]
    public void ToEquatableArray_FromReadOnlySpan_CreatesEquivalent()
    {
        ReadOnlySpan<int> span = [1, 2, 3];
        var result = span.ToEquatableArray();

        ShouldBeSequenceEqual(result, [1, 2, 3]);
    }

    [TestMethod]
    public void CollectionBuilder_EmptyArray_ReturnsEmptyArray()
    {
        EquatableArray<int> result = [];

        ShouldBeSequenceEqual(result, []);
    }

    [TestMethod]
    public void CollectionBuilder_SingleItem_CreatesCorrectArray()
    {
        EquatableArray<int> result = [42];

        ShouldBeSequenceEqual(result, [42]);
    }

    [TestMethod]
    public void CollectionBuilder_MultipleItems_CreatesCorrectArray()
    {
        EquatableArray<int> result = [1, 2, 3, 4, 5];

        ShouldBeSequenceEqual(result, [1, 2, 3, 4, 5]);
    }

    [TestMethod]
    public void Equals_SameInstance_ReturnsTrue()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        array.Equals(array).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_Null_ReturnsFalse()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        array.Equals(null).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_SameValues_ReturnsTrue()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var array2 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        array1.Equals(array2).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_DifferentValues_ReturnsFalse()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var array2 = EquatableArray.Create(ImmutableArray.Create(1, 2, 4));

        array1.Equals(array2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_DifferentLengths_ReturnsFalse()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var array2 = EquatableArray.Create(ImmutableArray.Create(1, 2));

        array1.Equals(array2).ShouldBeFalse();
    }

    [TestMethod]
    public void Equals_BothEmpty_ReturnsTrue()
    {
        var array1 = EquatableArray<int>.Empty;
        var array2 = EquatableArray<int>.Empty;

        array1.Equals(array2).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_Object_SameValues_ReturnsTrue()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        object array2 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        array1.Equals(array2).ShouldBeTrue();
    }

    [TestMethod]
    public void Equals_Object_DifferentType_ReturnsFalse()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        array.Equals("not an array").ShouldBeFalse();
    }

    [TestMethod]
    public void EqualityOperator_SameValues_ReturnsTrue()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var array2 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        (array1 == array2).ShouldBeTrue();
    }

    [TestMethod]
    public void EqualityOperator_DifferentValues_ReturnsFalse()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var array2 = EquatableArray.Create(ImmutableArray.Create(1, 2, 4));

        (array1 == array2).ShouldBeFalse();
    }

    [TestMethod]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        EquatableArray<int>? array1 = null;
        EquatableArray<int>? array2 = null;

        (array1 == array2).ShouldBeTrue();
    }

    [TestMethod]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        EquatableArray<int>? array2 = null;

        (array1 == array2).ShouldBeFalse();
        (array2 == array1).ShouldBeFalse();
    }

    [TestMethod]
    public void InequalityOperator_DifferentValues_ReturnsTrue()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var array2 = EquatableArray.Create(ImmutableArray.Create(1, 2, 4));

        (array1 != array2).ShouldBeTrue();
    }

    [TestMethod]
    public void InequalityOperator_SameValues_ReturnsFalse()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var array2 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        (array1 != array2).ShouldBeFalse();
    }

    [TestMethod]
    public void GetHashCode_SameValues_ReturnsSameHash()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var array2 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        array1.GetHashCode().ShouldBe(array2.GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_CalledMultipleTimes_ReturnsSameValue()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        int hash1 = array.GetHashCode();
        int hash2 = array.GetHashCode();
        int hash3 = array.GetHashCode();

        hash1.ShouldBe(hash2);
        hash2.ShouldBe(hash3);
    }

    [TestMethod]
    public void GetHashCode_DifferentValues_LikelyDifferentHash()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var array2 = EquatableArray.Create(ImmutableArray.Create(4, 5, 6));

        // Note: Hash collisions are possible, but very unlikely for different values
        array1.GetHashCode().ShouldNotBe(array2.GetHashCode());
    }

    [TestMethod]
    public void Equals_AfterEquality_ValuesRemainCorrect()
    {
        var array1 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var array2 = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        // Perform equality check
        array1.Equals(array2).ShouldBeTrue();

        // Verify values are still accessible and correct
        ShouldBeSequenceEqual(array1, [1, 2, 3]);
        ShouldBeSequenceEqual(array2, [1, 2, 3]);
    }

    [TestMethod]
    public void Indexer_OutOfRange_ThrowsException()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        Should.Throw<IndexOutOfRangeException>(() => _ = array[5]);
    }

    [TestMethod]
    public void Foreach_EnumeratesAllItems()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));
        var items = new List<int>();

        foreach (int item in array)
        {
            items.Add(item);
        }

        items.ShouldBe([1, 2, 3]);
    }

    [TestMethod]
    public void UnderlyingArray_ReturnsEquivalentImmutableArray()
    {
        var original = ImmutableArray.Create(1, 2, 3);
        var array = new EquatableArray<int>(original);

        ShouldBeSequenceEqual(array, [1, 2, 3]);
    }

    [TestMethod]
    public void UnderlyingArray_Empty_ReturnsEmptyImmutableArray()
    {
        var array = EquatableArray<int>.Empty;

        ShouldBeSequenceEqual(array, []);
    }

    [TestMethod]
    public void AsSpan_ReturnsCorrectSpan()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 3));

        ReadOnlySpan<int> span = array.AsSpan();

        span.Length.ShouldBe(3);
        span[0].ShouldBe(1);
        span[1].ShouldBe(2);
        span[2].ShouldBe(3);
    }

    [TestMethod]
    public void AsSpan_WithRange_ReturnsCorrectSpan()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 3, 4, 5));

        ReadOnlySpan<int> span = array.AsSpan(1..4);

        span.Length.ShouldBe(3);
        span[0].ShouldBe(2);
        span[1].ShouldBe(3);
        span[2].ShouldBe(4);
    }

    [TestMethod]
    public void Slice_ReturnsCorrectSlice()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 3, 4, 5));

        EquatableArray<int> slice = array.Slice(1, 3);

        ShouldBeSequenceEqual(slice, [2, 3, 4]);
    }

    [TestMethod]
    public void Slice_Empty_ReturnsEmptyArray()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 3, 4, 5));

        EquatableArray<int> slice = array.Slice(2, 0);

        ShouldBeSequenceEqual(slice, []);
    }

    [TestMethod]
    public void ToString_Empty_ReturnsExpectedFormat()
    {
        var array = EquatableArray<int>.Empty;

        string result = array.ToString(CultureInfo.InvariantCulture);

        result.ShouldBe("EquatableArray<Int32>[0] { }");
    }

    [TestMethod]
    public void ToString_SingleItem_ReturnsExpectedFormat()
    {
        EquatableArray<int> array = [42];

        string result = array.ToString(CultureInfo.InvariantCulture);

        result.ShouldBe("EquatableArray<Int32>[1] { 42 }");
    }

    [TestMethod]
    public void ToString_MultipleItems_ReturnsExpectedFormat()
    {
        EquatableArray<int> array = [1, 2, 3];

        string result = array.ToString(CultureInfo.InvariantCulture);

        result.ShouldBe("EquatableArray<Int32>[3] { 1, 2, 3 }");
    }

    [TestMethod]
    public void ToString_WithStrings_ReturnsExpectedFormat()
    {
        EquatableArray<string> array = ["hello", "world"];

        string result = array.ToString(CultureInfo.InvariantCulture);

        result.ShouldBe("EquatableArray<String>[2] { hello, world }");
    }

    [TestMethod]
    public void ToString_WithFormatProvider_UsesProvider()
    {
        EquatableArray<double> array = [1.5, 2.5];

        string result = array.ToString(CultureInfo.InvariantCulture);

        result.ShouldBe("EquatableArray<Double>[2] { 1.5, 2.5 }");
    }

    [TestMethod]
    public void IFormattable_ToString_ReturnsExpectedFormat()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(1, 2, 15));

        string result = array.ToString("X", CultureInfo.InvariantCulture);

        result.ShouldBe("EquatableArray<Int32>[3] { 1, 2, F }");
    }

    [TestMethod]
    public void Create_WithSingleNullString_HasCorrectValue()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(default(string?)));

        ShouldBeSequenceEqual(array, [default]);
    }

    [TestMethod]
    public void Create_WithSingleEnum_HasCorrectValue()
    {
        var array = EquatableArray.Create(ImmutableArray.Create(TestEnum.Value1));

        ShouldBeSequenceEqual(array, [TestEnum.Value1]);
    }

    private enum TestEnum
    {
        Value1 = 1,
        Value2 = 2,
        LargeValue = 1000,
    }

    [TestMethod]
    public void WithNullableValueType_HandlesNullCorrectly()
    {
        EquatableArray<int?> array = [1, null, 3];

        ShouldBeSequenceEqual(array, [1, null, 3]);
    }

    [TestMethod]
    public void WithNullStrings_HandlesNullCorrectly()
    {
        EquatableArray<string?> array = ["a", null, "c"];

        ShouldBeSequenceEqual(array, ["a", null, "c"]);
    }

    [TestMethod]
    public void LargeArray_WorksCorrectly()
    {
        var values = ImmutableArray.ToImmutableArray(Enumerable.Range(0, 1000));
        var array = EquatableArray.Create(values);

        ShouldBeSequenceEqual(array, [.. Enumerable.Range(0, 1000)]);
    }

    [TestMethod]
    public void CanBeUsedAsHashSetKey()
    {
        EquatableArray<int> array1 = [1, 2, 3];
        EquatableArray<int> array2 = [1, 2, 3];
        EquatableArray<int> array3 = [4, 5, 6];

        var set = new HashSet<EquatableArray<int>> { array1, array3 };

        set.Contains(array1).ShouldBeTrue();
        set.Contains(array2).ShouldBeTrue();
        set.Contains(array3).ShouldBeTrue();
        set.Count.ShouldBe(2);
    }

    [TestMethod]
    public void CanBeUsedAsDictionaryKey()
    {
        EquatableArray<int> array1 = [1, 2, 3];
        EquatableArray<int> array2 = [1, 2, 3];
        EquatableArray<int> array3 = [4, 5, 6];

        var dict = new Dictionary<EquatableArray<int>, string>
        {
            [array1] = "first",
            [array3] = "second",
        };

        dict[array1].ShouldBe("first");
        dict[array2].ShouldBe("first");
        dict[array3].ShouldBe("second");
        dict.Count.ShouldBe(2);
    }

    [TestMethod]
    public void Equality_WithCustomReferenceType_UsesDefaultEquality()
    {
        var obj1 = new TestObject(1);
        var obj2 = new TestObject(1);

        var array1 = EquatableArray.Create(ImmutableArray.Create(obj1));
        var array2 = EquatableArray.Create(ImmutableArray.Create(obj2));

        // TestObject uses value equality
        array1.Equals(array2).ShouldBeTrue();
    }

    private sealed class TestObject(int value) : IEquatable<TestObject>
    {
        public int Value { get; } = value;

        public bool Equals(TestObject? other) => other is not null && Value == other.Value;
        public override bool Equals(object? obj) => Equals(obj as TestObject);
        public override int GetHashCode() => Value.GetHashCode();
    }
}
