namespace Singulink.Collections.Tests.HashSetDictionary;

[PrefixTestClass]
public class FlowTests
{
    [TestMethod]
    public void FullFlow()
    {
        var d = new HashSetDictionary<int, string>(null, StringComparer.OrdinalIgnoreCase);
        var oneSet = d[1];
        var twoSet = d[2];

        d[1].AddRange(new[] { "one", "uno", "1" }).ShouldBe(3);
        d[2].AddRange(new[] { "two", "dos", "2" }).ShouldBe(3);

        d.Count.ShouldBe(2);
        d.Keys.Count.ShouldBe(2);
        d.ValueCount.ShouldBe(6);
        d.Values.Count.ShouldBe(6);

        // Duplicates

        d[1].AddRange(new[] { "ONE", "One" });
        d[2].AddRange(new[] { "TWO", "Two" });

        d.Count.ShouldBe(2);
        d.Keys.Count.ShouldBe(2);
        d.ValueCount.ShouldBe(6);
        d.Values.Count.ShouldBe(6);

        // Value set sync

        d[1].ShouldNotBeSameAs(oneSet);
        d[2].ShouldNotBeSameAs(twoSet);

        d[1].ShouldBe(oneSet);
        d[2].ShouldBe(twoSet);

        // Add new key with duplicates

        d[3].AddRange(new[] { "three", "tres", "3", "THREE", "Three" });

        d.Count.ShouldBe(3);
        d.Keys.Count.ShouldBe(3);
        d.ValueCount.ShouldBe(9);
        d.Values.Count.ShouldBe(9);

        // GetValueCount

        d.GetValueCount(3).ShouldBe(3);

        // ContainsKey false

        d.ContainsKey(4).ShouldBeFalse();
        d[4].AddRange(new string[] { });
        d.ContainsKey(4).ShouldBeFalse();

        // TryGetValues

        d.TryGetValues(2, out var twoSetDup).ShouldBe(true);

        twoSetDup.ShouldNotBeNull();
        twoSetDup.Key.ShouldBe(2);
        twoSetDup.Count.ShouldBe(3);
        twoSetDup.ShouldBe(new[] { "two", "dos", "2" }, ignoreOrder: true);
        twoSetDup.AsTransientReadOnly().ShouldBe(new[] { "two", "dos", "2" }, ignoreOrder: true);
        twoSetDup.ShouldBe(twoSet);

        twoSet.Count.ShouldBe(3);
        twoSet.ShouldBe(new[] { "two", "dos", "2" }, ignoreOrder: true);
        twoSet.AsTransientReadOnly().ShouldBe(new[] { "two", "dos", "2" }, ignoreOrder: true);

        // Remove

        twoSet.Remove("DOS").ShouldBe(true);

        d.Count.ShouldBe(3);
        d.Keys.Count.ShouldBe(3);
        d.ValueCount.ShouldBe(8);
        d.Values.Count.ShouldBe(8);

        d.Keys.ShouldBe(new[] { 1, 2, 3 }, ignoreOrder: true);
        d.Values.ShouldBe(new[] { "one", "uno", "1", "two", "2", "three", "tres", "3" }, ignoreOrder: true);

        twoSet.Count.ShouldBe(2);
        twoSet.ShouldBe(new[] { "two", "2" }, ignoreOrder: true);
        twoSet.AsTransientReadOnly().ShouldBe(new[] { "two", "2" }, ignoreOrder: true);

        twoSetDup.ShouldBe(twoSet);

        // ExceptWith

        d[2].ExceptWith(new[] { "Two", "2", "NotInThere" });

        d.Count.ShouldBe(2);
        d.Keys.Count.ShouldBe(2);
        d.ValueCount.ShouldBe(6);
        d.Values.Count.ShouldBe(6);

        d.Keys.ShouldBe(new[] { 1, 3 }, ignoreOrder: true);
        d.Values.ShouldBe(new[] { "one", "uno", "1", "three", "tres", "3" }, ignoreOrder: true);

        d.ContainsKey(2).ShouldBeFalse();
        twoSet.Count.ShouldBe(0);
        twoSetDup.Count.ShouldBe(0);

        d.TryGetValues(2, out var twoSetNull).ShouldBe(false);
        twoSetNull.ShouldBeNull();

        // Clear(key)

        var threeSet = d[3];
        d.Clear(3);

        d.Count.ShouldBe(1);
        d.Keys.Count.ShouldBe(1);
        d.ValueCount.ShouldBe(3);
        d.Values.Count.ShouldBe(3);

        d.ContainsKey(3).ShouldBeFalse();
        threeSet.Count.ShouldBe(0);

        // Clear

        oneSet.Count.ShouldBe(3);
        d.Clear();
        oneSet.Count.ShouldBe(0);

        d.Count.ShouldBe(0);
        d.ValueCount.ShouldBe(0);
        d.Keys.Count.ShouldBe(0);
        d.Values.Count.ShouldBe(0);
    }
}