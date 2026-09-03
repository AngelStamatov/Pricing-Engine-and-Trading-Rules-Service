using PricingAndTrading.Application.Pricing;
using PricingAndTrading.Domain.Prices;
using PricingAndTrading.Infrastructure.Pricing;

namespace PricingAndTrading.IntegrationTests.Prices;

public sealed class ChannelPriceFeedTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 9, 3, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task PublishAndReadAsync_OnePriceTick_ReturnsPublishedTickThroughBothContracts()
    {
        var feed = new ChannelPriceFeed(capacity: 1);
        IPriceTickPublisher publisher = feed;
        IPriceFeed consumer = feed;
        var expected = new PriceTick("EURUSD", 99m, 101m, Timestamp);

        await publisher.PublishAsync(expected);

        PriceTick actual = await ReadOneAsync(consumer);
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task PublishAndReadAsync_MultipleConcurrentProducers_DeliversEveryTick()
    {
        const int producerCount = 5;
        const int ticksPerProducer = 20;
        const int expectedCount = producerCount * ticksPerProducer;
        var feed = new ChannelPriceFeed(capacity: 4);

        Task<PriceTick[]> consumer = ReadManyAsync(feed, expectedCount);
        Task[] producers = Enumerable.Range(0, producerCount)
            .Select(index => WritePricesAsync(feed, index, ticksPerProducer))
            .ToArray();

        await Task.WhenAll(producers);
        PriceTick[] received = await consumer.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(expectedCount, received.Length);
        Assert.Equal(producerCount, received.Select(price => price.Symbol).Distinct().Count());
    }

    [Fact]
    public async Task ReadAllAsync_CancelledToken_StopsWaitingReader()
    {
        var feed = new ChannelPriceFeed(capacity: 1);
        using var cancellation = new CancellationTokenSource();
        await using IAsyncEnumerator<PriceTick> reader =
            feed.ReadAllAsync(cancellation.Token).GetAsyncEnumerator();
        Task<bool> pendingRead = reader.MoveNextAsync().AsTask();

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pendingRead);
    }

    [Fact]
    public async Task PublishAsync_FullChannel_WaitsForCapacityWithoutDroppingTicks()
    {
        var feed = new ChannelPriceFeed(capacity: 1);
        var first = new PriceTick("EURUSD", 99m, 101m, Timestamp);
        var second = new PriceTick("GBPUSD", 119m, 121m, Timestamp);
        await feed.PublishAsync(first);

        Task blockedWrite = feed.PublishAsync(second).AsTask();
        Assert.False(blockedWrite.IsCompleted);

        PriceTick firstRead = await ReadOneAsync(feed);
        await blockedWrite.WaitAsync(TimeSpan.FromSeconds(1));
        PriceTick secondRead = await ReadOneAsync(feed);

        Assert.Same(first, firstRead);
        Assert.Same(second, secondRead);
    }

    private static async Task WritePricesAsync(
        ChannelPriceFeed feed,
        int producerIndex,
        int count)
    {
        for (var index = 0; index < count; index++)
        {
            decimal bidPrice = 100m + producerIndex + (index / 100m);
            await feed.PublishAsync(new PriceTick(
                $"SYM{producerIndex}",
                bidPrice,
                bidPrice + 0.01m,
                Timestamp));
        }
    }

    private static async Task<PriceTick[]> ReadManyAsync(
        ChannelPriceFeed feed,
        int count)
    {
        var prices = new List<PriceTick>(count);

        await foreach (PriceTick priceTick in feed.ReadAllAsync(CancellationToken.None))
        {
            prices.Add(priceTick);
            if (prices.Count == count)
            {
                break;
            }
        }

        return prices.ToArray();
    }

    private static async Task<PriceTick> ReadOneAsync(IPriceFeed feed)
    {
        await using IAsyncEnumerator<PriceTick> reader =
            feed.ReadAllAsync(CancellationToken.None).GetAsyncEnumerator();

        Assert.True(await reader.MoveNextAsync());
        return reader.Current;
    }
}
