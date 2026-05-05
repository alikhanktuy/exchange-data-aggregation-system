using Domain.Entities;
using Infrastructure.Persistence;

namespace Tests.Unit;

/// <summary>Unit tests for tick repository batch behavior.</summary>
public sealed class TickRepositoryTests
{
    /// <summary>InsertBatchAsync invokes the binary-copy path once and passes all rows.</summary>
    [Fact]
    public async Task InsertBatchInvokesCopyOnceWithAllRows()
    {
        var repository = new TestableTickRepository();
        var batch = Enumerable.Range(0, 3)
            .Select(i => new Tick("BTCUSDT", i, 1m, DateTimeOffset.UtcNow.AddMilliseconds(i), "ExchangeA"))
            .ToArray();

        await repository.InsertBatchAsync(batch);

        Assert.Equal(1, repository.CopyCalls);
        Assert.Equal(3, repository.RowCount);
    }

    private sealed class TestableTickRepository : TickRepository
    {
        public TestableTickRepository()
            : base(null!)
        {
        }

        public int CopyCalls { get; private set; }

        public int RowCount { get; private set; }

        protected override Task CopyAsync(IReadOnlyCollection<Tick> batch, CancellationToken ct)
        {
            CopyCalls++;
            RowCount += batch.Count;
            return Task.CompletedTask;
        }
    }
}
