namespace Singulink.Collections.Tests.ListDictionary;

[PrefixTestClass]
public class FlowTests
{
    [TestMethod]
    public void FullFlow()
    {
        var d = new ListDictionary<int, string>();
        var oneList = d[1];
        var twoList = d[2];

        d[1].Add("one");
        d[1].AddRange(new[] { "uno", "1" }).ShouldBe(2);
        d[2].AddRange(new[] { "two", "dos", "222" }).ShouldBe(3);
        d[2][2] = "2";
        d[3].AddRange(new[] { "three", "tres", "3" }).ShouldBe(3);

        d.Count.ShouldBe(3);
        d.Keys.Count.ShouldBe(3);
        d.ValueCount.ShouldBe(9);
        d.Values.Count.ShouldBe(9);

        // Value list sync

        d[1].ShouldNotBeSameAs(oneList);
        d[2].ShouldNotBeSameAs(twoList);

        d[1].ShouldBe(oneList);
        d[2].ShouldBe(twoList);

        // GetValueCount

        d.GetValueCount(3).ShouldBe(3);

        // ContainsKey false

        d.ContainsKey(4).ShouldBeFalse();
        d[4].AddRange(new string[0]);
        d.ContainsKey(4).ShouldBeFalse();

        // TryGetValues

        d.TryGetValues(2, out var twoSetDup).ShouldBe(true);

        twoSetDup.ShouldNotBeNull();
        twoSetDup.Key.ShouldBe(2);
        twoSetDup.Count.ShouldBe(3);
        twoSetDup.ShouldBe(new[] { "two", "dos", "2" });
        twoSetDup.AsTransientReadOnly().ShouldBe(new[] { "two", "dos", "2" });
        twoSetDup.ShouldBe(twoList);

        twoList.Count.ShouldBe(3);
        twoList.ShouldBe(new[] { "two", "dos", "2" });
        twoList.AsTransientReadOnly().ShouldBe(new[] { "two", "dos", "2" });

        // Remove

        twoList.Remove("DOS").ShouldBe(false);
        twoList.Remove("dos").ShouldBe(true);

        d.Count.ShouldBe(3);
        d.Keys.Count.ShouldBe(3);
        d.ValueCount.ShouldBe(8);
        d.Values.Count.ShouldBe(8);

        d.Keys.ShouldBe(new[] { 1, 2, 3 }, ignoreOrder: true);
        d.Values.ShouldBe(new[] { "one", "uno", "1", "two", "2", "three", "tres", "3" }, ignoreOrder: true);

        twoList.Count.ShouldBe(2);
        twoList.ShouldBe(new[] { "two", "2" });
        twoList.AsTransientReadOnly().ShouldBe(new[] { "two", "2" });

        twoSetDup.ShouldBe(twoList);

        // SetRange

        d[2].SetRange(new string[0]);

        d.Count.ShouldBe(2);
        d.Keys.Count.ShouldBe(2);
        d.ValueCount.ShouldBe(6);
        d.Values.Count.ShouldBe(6);

        d.Keys.ShouldBe(new[] { 1, 3 }, ignoreOrder: true);
        d.Values.ShouldBe(new[] { "one", "uno", "1", "three", "tres", "3" }, ignoreOrder: true);

        d.ContainsKey(2).ShouldBeFalse();
        twoList.Count.ShouldBe(0);
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

        oneList.Count.ShouldBe(3);
        d.Clear();
        oneList.Count.ShouldBe(0);

        d.Count.ShouldBe(0);
        d.ValueCount.ShouldBe(0);
        d.Keys.Count.ShouldBe(0);
        d.Values.Count.ShouldBe(0);
    }
}