namespace Singulink.Collections.Tests.Map;

[PrefixTestClass]
public class FlowTests
{
    [TestMethod]
    public void FullFlow()
    {
        var map = new Map<int, string>(null, StringComparer.OrdinalIgnoreCase) {
            { 1, "one" },
            { 2, "two" },
            { 3, "three" },
        };

        var reverse = map.Reverse;

        map.Count.ShouldBe(3);
        reverse.Count.ShouldBe(3);

        map.Add(4, "four");

        map.Count.ShouldBe(4);
        reverse.Count.ShouldBe(4);

        map.RemoveRight("ONE").ShouldBe(true);
        map.Reverse.Remove("TWO", 3).ShouldBe(false);
        map.Remove(2, "TWO").ShouldBe(true);

        map.Count.ShouldBe(2);
        reverse.Count.ShouldBe(2);

        map.ShouldBe(new KeyValuePair<int, string>[] { new(3, "three"), new(4, "four") }, ignoreOrder: true);
        map.Reverse.ShouldBe(new KeyValuePair<string, int>[] { new("three", 3), new("four", 4) }, ignoreOrder: true);
        map.LeftValues.ShouldBe(new[] { 3, 4 }, ignoreOrder: true);
        map.RightValues.ShouldBe(new[] { "three", "four" }, ignoreOrder: true);

        Should.Throw<ArgumentException>(() => map[3] = "FOUR");
        Should.Throw<ArgumentException>(() => map[4] = "Three");

        map[3] = "3";
        map[4] = "4";

        map.Count.ShouldBe(2);
        reverse.Count.ShouldBe(2);

        map.LeftValues.ShouldBe(new[] { 3, 4 }, ignoreOrder: true);
        map.RightValues.ShouldBe(new[] { "3", "4" }, ignoreOrder: true);
        map.ShouldBe(new KeyValuePair<int, string>[] { new(3, "3"), new(4, "4") }, ignoreOrder: true);
        map.Reverse.ShouldBe(new KeyValuePair<string, int>[] { new("3", 3), new("4", 4) }, ignoreOrder: true);

        map.Clear();

        map.Count.ShouldBe(0);
        map.Reverse.Count.ShouldBe(0);
        map.LeftValues.Count.ShouldBe(0);
        map.LeftValues.Count.ShouldBe(0);
    }
}