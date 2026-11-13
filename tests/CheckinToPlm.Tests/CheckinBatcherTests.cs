using System.Collections.Concurrent;

namespace CheckinToPlm.Tests;

public class CheckinBatcherTests
{
    private static readonly TimeSpan Quiet = TimeSpan.FromMilliseconds(150);

    [Fact]
    public void Burst_of_checkins_becomes_one_batch_without_duplicates()
    {
        var batches = new ConcurrentQueue<long[]>();
        using var done = new ManualResetEventSlim();
        using var batcher = new CheckinBatcher<object>(Quiet, (_, ids) =>
        {
            batches.Enqueue(ids.ToArray());
            done.Set();
        });
        var session = new object();

        batcher.Add(session, 1);
        batcher.Add(session, 2);
        batcher.Add(session, 2);
        batcher.Add(session, 3);

        Assert.True(done.Wait(TimeSpan.FromSeconds(5)));
        Thread.Sleep(Quiet * 3);
        Assert.Equal([1L, 2L, 3L], Assert.Single(batches));
    }

    [Fact]
    public void Each_new_checkin_restarts_the_quiet_period()
    {
        var fired = 0;
        using var batcher = new CheckinBatcher<object>(TimeSpan.FromMilliseconds(400), (_, _) => Interlocked.Increment(ref fired));
        var session = new object();

        for (var i = 0; i < 5; i++)
        {
            batcher.Add(session, i);
            Thread.Sleep(100);
        }

        Assert.Equal(0, Volatile.Read(ref fired));
        Thread.Sleep(1200);
        Assert.Equal(1, Volatile.Read(ref fired));
    }

    [Fact]
    public void Separate_sessions_are_processed_separately()
    {
        var seen = new ConcurrentDictionary<object, long[]>();
        using var done = new CountdownEvent(2);
        using var batcher = new CheckinBatcher<object>(Quiet, (context, ids) =>
        {
            seen[context] = ids.ToArray();
            done.Signal();
        });
        var first = new object();
        var second = new object();

        batcher.Add(first, 1);
        batcher.Add(second, 2);

        Assert.True(done.Wait(TimeSpan.FromSeconds(5)));
        Assert.Equal([1L], seen[first]);
        Assert.Equal([2L], seen[second]);
    }

    [Fact]
    public void A_failing_batch_does_not_stop_later_batches()
    {
        var calls = 0;
        using var second = new ManualResetEventSlim();
        using var batcher = new CheckinBatcher<object>(Quiet, (_, _) =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                throw new InvalidOperationException("boom");
            }

            second.Set();
        });
        var session = new object();

        batcher.Add(session, 1);
        Thread.Sleep(Quiet * 4);
        batcher.Add(session, 2);

        Assert.True(second.Wait(TimeSpan.FromSeconds(5)));
    }
}
