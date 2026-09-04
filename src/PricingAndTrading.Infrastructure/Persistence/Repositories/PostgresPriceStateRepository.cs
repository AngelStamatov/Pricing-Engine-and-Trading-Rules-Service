using Microsoft.EntityFrameworkCore;
using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Domain.Prices;
using PricingAndTrading.Infrastructure.Persistence.Entities;

namespace PricingAndTrading.Infrastructure.Persistence.Repositories;

internal sealed class PostgresPriceStateRepository : IPriceStateRepository
{
    private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;

    public PostgresPriceStateRepository(
        IDbContextFactory<TradingDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<MarketPrice>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        await using TradingDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<LatestPriceEntity> entities = await dbContext.LatestPrices
            .AsNoTracking()
            .OrderBy(price => price.Symbol)
            .ToListAsync(cancellationToken);

        return entities.Select(PersistenceMapper.ToDomain).ToArray();
    }

    public async Task UpsertLatestAsync(
        IReadOnlyCollection<MarketPrice> prices,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prices);

        if (prices.Count == 0)
        {
            return;
        }

        var normalizedSymbols = new HashSet<string>(StringComparer.Ordinal);

        foreach (MarketPrice price in prices)
        {
            ArgumentNullException.ThrowIfNull(price);

            string normalizedSymbol = price.Symbol.Trim().ToUpperInvariant();
            if (!normalizedSymbols.Add(normalizedSymbol))
            {
                throw new ArgumentException(
                    "Prices must not contain duplicate normalized symbols.",
                    nameof(prices));
            }
        }

        string[] symbols = normalizedSymbols.ToArray();

        await using TradingDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        Dictionary<string, LatestPriceEntity> existingPrices =
            await dbContext.LatestPrices
                .Where(price => symbols.Contains(price.Symbol))
                .ToDictionaryAsync(
                    price => price.Symbol,
                    StringComparer.Ordinal,
                    cancellationToken);

        // A read-then-upsert is intentionally simple for the current
        // small, single-instance latest-price snapshot.
        foreach (MarketPrice price in prices)
        {
            if (existingPrices.TryGetValue(price.Symbol, out LatestPriceEntity? entity))
            {
                PersistenceMapper.Update(entity, price);
            }
            else
            {
                dbContext.LatestPrices.Add(PersistenceMapper.ToEntity(price));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
