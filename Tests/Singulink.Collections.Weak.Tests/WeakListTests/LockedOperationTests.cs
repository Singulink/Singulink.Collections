namespace Singulink.Collections.Weak.Tests.WeakListTests;

[PrefixTestClass]
public class LockedOperationTests
{
    [TestMethod]
    public void PerformLockedOperationPassesListAndState()
    {
        var list = new WeakList<object>();
        var state = new object();
        bool ran = false;

        list.UnsafePerformLockedOperation(state, (l, s) => {
            l.ShouldBeSameAs(list);
            s.ShouldBeSameAs(state);
            ran = true;
        });

        ran.ShouldBeTrue();
    }

    [TestMethod]
    public void PerformLockedOperationAllowsCompoundOperations()
    {
        var list = new WeakList<object>();
        var first = new object();
        var second = new object();

        list.UnsafePerformLockedOperation((first, second), (l, s) => {
            l.AddLast(s.first);
            l.AddLast(s.second);
            l.Count.ShouldBe(2);
            l.Remove(s.first).ShouldBeTrue();
            l.Contains(s.second).ShouldBeTrue();
        });

        list.Count.ShouldBe(1);
        list.ShouldBe([second]);
        GC.KeepAlive(first);
    }

    [TestMethod]
    public void TryPerformLockedOperationSucceedsWhenUncontended()
    {
        var list = new WeakList<object>();
        bool ran = false;

        list.UnsafeTryPerformLockedOperation(5, (l, s) => {
            s.ShouldBe(5);
            ran = true;
        }).ShouldBeTrue();

        ran.ShouldBeTrue();
    }

    [TestMethod]
    public void TryPerformLockedOperationFailsWhileAnotherThreadHoldsLock()
    {
        var list = new WeakList<object>();
        bool? innerResult = null;

        list.UnsafePerformLockedOperation(0, (l, _) => {
            // A dedicated thread is used instead of a task because waiting on an unstarted task can inline it onto this thread, which holds the lock.
            var thread = new Thread(() => innerResult = l.UnsafeTryPerformLockedOperation(0, (_, _) => { }));
            thread.Start();
            thread.Join();
        });

        innerResult.ShouldBe(false);

        // The lock is released after the operation completes.
        list.UnsafeTryPerformLockedOperation(0, (_, _) => { }).ShouldBeTrue();
    }

    [TestMethod]
    public void ExceptionInOperationReleasesLock()
    {
        var list = new WeakList<object>();

        Should.Throw<InvalidOperationException>(() => list.UnsafePerformLockedOperation(0, (_, _) => throw new InvalidOperationException()));
        Should.Throw<InvalidOperationException>(() => list.UnsafeTryPerformLockedOperation(0, (_, _) => throw new InvalidOperationException()));

        bool? result = null;
        var thread = new Thread(() => result = list.UnsafeTryPerformLockedOperation(0, (_, _) => { }));
        thread.Start();
        thread.Join();
        result.ShouldBe(true);
        list.AddLast(new object());
        list.Count.ShouldBe(1);
    }

    [TestMethod]
    public void LockedOperationsAfterDispose()
    {
        var list = new WeakList<object>();
        list.Dispose();

        Should.Throw<ObjectDisposedException>(() => list.UnsafePerformLockedOperation(0, (_, _) => { }));

        bool ran = false;
        list.UnsafeTryPerformLockedOperation(0, (_, _) => ran = true).ShouldBeFalse();
        ran.ShouldBeFalse();
    }

    [TestMethod]
    public void OtherThreadsBlockUntilLockedOperationCompletes()
    {
        var list = new WeakList<object>();
        var value = new object();
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        var locked = new Thread(() => list.UnsafePerformLockedOperation(0, (l, _) => {
            started.Set();
            release.Wait();
            l.AddLast(value);
        }));

        locked.Start();
        started.Wait();

        var adder = new Thread(() => list.AddLast(new object()));
        adder.Start();
        adder.Join(100).ShouldBeFalse();

        release.Set();
        locked.Join();
        adder.Join();

        list.Count.ShouldBe(2);
        list.First().ShouldBeSameAs(value);
        GC.KeepAlive(value);
    }
}
