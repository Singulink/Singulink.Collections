using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Singulink.Collections.Tests.Extensions;

[PrefixTestClass]
public class CollectionExtensionsTests
{
    [TestMethod]
    public void AsSafeEnumerableYieldsAllEntries()
    {
        var dictionary = new ConcurrentDictionary<int, string>();
        dictionary[1] = "one";
        dictionary[2] = "two";

        var safe = dictionary.AsSafeEnumerable();

        safe.ShouldBe([new(1, "one"), new(2, "two")], ignoreOrder: true);
        safe.ShouldNotBeSameAs(dictionary);
        safe.Count().ShouldBe(2);
    }

    [TestMethod]
    public void ToEquatableArrayFromArrayAndSpan()
    {
        int[] source = [1, 2, 3];

        var fromArray = source.ToEquatableArray();
        var fromSpan = new ReadOnlySpan<int>(source).ToEquatableArray();
        var fromEnumerable = source.Select(v => v).ToEquatableArray();
        var fromImmutable = ImmutableArray.Create(1, 2, 3).ToEquatableArray();

        fromArray.ShouldBe(fromSpan);
        fromArray.ShouldBe(fromEnumerable);
        fromArray.ShouldBe(fromImmutable);
        fromArray.Length.ShouldBe(3);

        source[0] = 99;
        fromArray[0].ShouldBe(1);
    }

    [TestMethod]
    public void ToComparerEquatableArrayFromArrayAndSpan()
    {
        string[] source = ["a", "B"];

        var fromArray = source.ToComparerEquatableArray(StringComparer.OrdinalIgnoreCase);
        var fromSpan = new ReadOnlySpan<string>(source).ToComparerEquatableArray(StringComparer.OrdinalIgnoreCase);
        var fromEnumerable = source.Select(v => v).ToComparerEquatableArray(StringComparer.OrdinalIgnoreCase);
        var fromImmutable = ImmutableArray.Create("A", "b").ToComparerEquatableArray(StringComparer.OrdinalIgnoreCase);

        fromArray.Comparer.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
        fromArray.Equals(fromSpan).ShouldBeTrue();
        fromArray.Equals(fromEnumerable).ShouldBeTrue();
        fromArray.Equals(fromImmutable).ShouldBeTrue();
        fromArray.Contains("A").ShouldBeTrue();

        source.ToComparerEquatableArray().Comparer.ShouldBe(EqualityComparer<string>.Default);
    }
}
