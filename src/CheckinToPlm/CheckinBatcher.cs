namespace CheckinToPlm;

/// <summary>
/// Collects checked-in file IDs and hands them over as one batch once no new
/// check-in has arrived for the quiet period. An assembly check-in raises one
/// event per file; this turns that burst into a single unit of work.
/// </summary>
internal sealed class CheckinBatcher<TContext> : IDisposable
    where TContext : class
{
    private readonly object _gate = new();
    private readonly TimeSpan _quietPeriod;
    private readonly Action<TContext, IReadOnlyList<long>> _process;
    private readonly System.Threading.Timer _timer;
    private readonly Dictionary<TContext, List<long>> _pending = new(ReferenceEqualityComparer.Instance);

    public CheckinBatcher(TimeSpan quietPeriod, Action<TContext, IReadOnlyList<long>> process)
    {
        _quietPeriod = quietPeriod;
        _process = process;
        _timer = new System.Threading.Timer(_ => Flush(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Add(TContext context, long fileId)
    {
        lock (_gate)
        {
            if (!_pending.TryGetValue(context, out var ids))
            {
                ids = [];
                _pending[context] = ids;
            }

            if (!ids.Contains(fileId))
            {
                ids.Add(fileId);
            }

            _timer.Change(_quietPeriod, Timeout.InfiniteTimeSpan);
        }
    }

    private void Flush()
    {
        List<KeyValuePair<TContext, List<long>>> batches;
        lock (_gate)
        {
            batches = _pending.ToList();
            _pending.Clear();
        }

        foreach (var batch in batches)
        {
            try
            {
                _process(batch.Key, batch.Value);
            }
            catch (Exception ex)
            {
                Log.Error("Batch processing failed", ex);
            }
        }
    }

    public void Dispose() => _timer.Dispose();
}
