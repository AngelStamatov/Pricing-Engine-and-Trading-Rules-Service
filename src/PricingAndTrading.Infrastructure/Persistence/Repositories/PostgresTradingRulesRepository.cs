using Microsoft.EntityFrameworkCore;
using PricingAndTrading.Application.Abstractions;
using PricingAndTrading.Infrastructure.Persistence.Entities;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.Infrastructure.Persistence.Repositories;

internal sealed class PostgresTradingRulesRepository : ITradingRulesRepository
{
    private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;

    public PostgresTradingRulesRepository(
        IDbContextFactory<TradingDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        _dbContextFactory = dbContextFactory;
    }

    public async Task<TradingRulesConfiguration?> GetAsync(
        CancellationToken cancellationToken)
    {
        await using TradingDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        TradingRulesEntity? entity = await dbContext.TradingRules
            .AsNoTracking()
            .SingleOrDefaultAsync(
                rules => rules.Id == TradingRulesEntity.ActiveConfigurationId,
                cancellationToken);

        return entity is null
            ? null
            : PersistenceMapper.ToDomain(entity);
    }

    public async Task SaveAsync(
        TradingRulesConfiguration tradingRules,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tradingRules);

        await using TradingDbContext dbContext =
            await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        TradingRulesEntity? entity = await dbContext.TradingRules
            .SingleOrDefaultAsync(
                rules => rules.Id == TradingRulesEntity.ActiveConfigurationId,
                cancellationToken);

        if (entity is null)
        {
            dbContext.TradingRules.Add(PersistenceMapper.ToEntity(tradingRules));
        }
        else
        {
            PersistenceMapper.Update(entity, tradingRules);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
