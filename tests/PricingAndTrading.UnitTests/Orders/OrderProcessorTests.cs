using PricingAndTrading.Application.Orders;
using PricingAndTrading.Application.TradingRules;
using PricingAndTrading.Domain.Orders;
using TradingRulesConfiguration = PricingAndTrading.Domain.TradingRules.TradingRules;

namespace PricingAndTrading.UnitTests.Orders;

public sealed class OrderProcessorTests
{
    [Fact]
    public async Task ProcessAsync_AllRulesPassAndOrderIdIsNew_ReturnsAcceptedAndPersistsOnce()
    {
        var context = new OrderProcessorTestContext();
        OrderProcessor processor = context.CreateProcessor();
        using var cancellationSource = new CancellationTokenSource();

        TradeDecision decision = await processor.ProcessAsync(
            context.Order,
            cancellationSource.Token);

        Assert.Equal(OrderStatus.Accepted, decision.Status);
        Assert.Empty(decision.RejectionReasons);
        Assert.Equal(1, context.OrderIdRegistry.CallCount);
        Assert.Equal(context.Order.Id, context.OrderIdRegistry.LastOrderId);
        Assert.Equal(cancellationSource.Token, context.OrderIdRegistry.LastCancellationToken);
        Assert.Equal(1, context.TradingRulesEngine.CallCount);
        Assert.Equal(1, context.OrderRepository.CallCount);
        Assert.Same(context.Order, context.OrderRepository.LastOrder);
        Assert.Same(decision, context.OrderRepository.LastDecision);
        Assert.Equal(cancellationSource.Token, context.OrderRepository.LastCancellationToken);
    }

    [Fact]
    public async Task ProcessAsync_OnePureRuleFails_ReturnsRejectedWithPreservedReason()
    {
        var context = new OrderProcessorTestContext();
        RejectionReason reason = CreateReason("MaximumQuantityExceeded");
        context.TradingRulesEngine.Result =
            TradeValidationResult.Invalid(reason);
        OrderProcessor processor = context.CreateProcessor();

        TradeDecision decision = await processor.ProcessAsync(context.Order);

        Assert.Equal(OrderStatus.Rejected, decision.Status);
        Assert.Same(reason, Assert.Single(decision.RejectionReasons));
        Assert.Equal(1, context.OrderRepository.CallCount);
        Assert.Same(decision, context.OrderRepository.LastDecision);
    }

    [Fact]
    public async Task ProcessAsync_MultiplePureRulesFail_PreservesAllReasons()
    {
        var context = new OrderProcessorTestContext();
        RejectionReason firstReason = CreateReason("MaximumNotionalExceeded");
        RejectionReason secondReason = CreateReason("PriceDeviationExceeded");
        context.TradingRulesEngine.Result =
            TradeValidationResult.Invalid(firstReason, secondReason);
        OrderProcessor processor = context.CreateProcessor();

        TradeDecision decision = await processor.ProcessAsync(context.Order);

        Assert.Equal(OrderStatus.Rejected, decision.Status);
        Assert.Equal(
            [firstReason, secondReason],
            decision.RejectionReasons);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateAndPureRulesFail_CombinesReasonsInDeterministicOrder()
    {
        var context = new OrderProcessorTestContext();
        context.OrderIdRegistry.TryRegisterResult = false;
        context.TradingRulesEngine.Result = TradeValidationResult.Invalid(
            CreateReason("MaximumQuantityExceeded"),
            CreateReason("PriceDeviationExceeded"));
        OrderProcessor processor = context.CreateProcessor();

        TradeDecision decision = await processor.ProcessAsync(context.Order);

        Assert.Equal(OrderStatus.Rejected, decision.Status);
        Assert.Equal(1, context.OrderIdRegistry.CallCount);
        Assert.Equal(1, context.TradingRulesEngine.CallCount);
        Assert.Equal(
            [
                "DuplicateOrderId",
                "MaximumQuantityExceeded",
                "PriceDeviationExceeded"
            ],
            decision.RejectionReasons.Select(static reason => reason.Code));
    }

    [Fact]
    public async Task ProcessAsync_DuplicateCheckDisabled_RegistersOrderIdWithoutRejecting()
    {
        var context = new OrderProcessorTestContext();
        context.TradingRulesProvider.CurrentValue =
            OrderProcessorTestContext.CreateTradingRules(
                duplicateOrderIdCheckEnabled: false);
        context.OrderIdRegistry.TryRegisterResult = false;
        OrderProcessor processor = context.CreateProcessor();

        TradeDecision decision = await processor.ProcessAsync(context.Order);

        Assert.Equal(OrderStatus.Accepted, decision.Status);
        Assert.Empty(decision.RejectionReasons);
        Assert.Equal(1, context.OrderIdRegistry.CallCount);
        Assert.Equal(context.Order.Id, context.OrderIdRegistry.LastOrderId);
        Assert.Equal(1, context.TradingRulesEngine.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateCheckEnabledAfterDisabledProcessing_RejectsPreviouslyRegisteredId()
    {
        var context = new OrderProcessorTestContext();
        context.TradingRulesProvider.CurrentValue =
            OrderProcessorTestContext.CreateTradingRules(
                duplicateOrderIdCheckEnabled: false);
        OrderProcessor processor = context.CreateProcessor();

        TradeDecision firstDecision =
            await processor.ProcessAsync(context.Order);

        context.TradingRulesProvider.CurrentValue =
            OrderProcessorTestContext.CreateTradingRules(
                duplicateOrderIdCheckEnabled: true);

        TradeDecision secondDecision =
            await processor.ProcessAsync(context.Order);

        Assert.Equal(OrderStatus.Accepted, firstDecision.Status);
        Assert.Equal(OrderStatus.Rejected, secondDecision.Status);
        RejectionReason reason = Assert.Single(secondDecision.RejectionReasons);
        Assert.Equal("DuplicateOrderId", reason.Code);
        Assert.Equal(2, context.OrderIdRegistry.CallCount);
        Assert.Equal(2, context.OrderRepository.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateCheckEnabledAndOrderIdExists_ReturnsDuplicateRejection()
    {
        var context = new OrderProcessorTestContext();
        context.OrderIdRegistry.TryRegisterResult = false;
        OrderProcessor processor = context.CreateProcessor();

        TradeDecision decision = await processor.ProcessAsync(context.Order);

        Assert.Equal(OrderStatus.Rejected, decision.Status);
        RejectionReason reason = Assert.Single(decision.RejectionReasons);
        Assert.Equal("DuplicateOrderId", reason.Code);
        Assert.Equal(1, context.OrderIdRegistry.CallCount);
        Assert.Equal(1, context.TradingRulesEngine.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_OneOrder_ReadsAndUsesSameRulesSnapshotOnce()
    {
        var context = new OrderProcessorTestContext();
        TradingRulesConfiguration expectedSnapshot =
            OrderProcessorTestContext.CreateTradingRules(
                duplicateOrderIdCheckEnabled: true);
        context.TradingRulesProvider.CurrentValue = expectedSnapshot;
        OrderProcessor processor = context.CreateProcessor();

        await processor.ProcessAsync(context.Order);

        Assert.Equal(1, context.TradingRulesProvider.ReadCount);
        Assert.Same(
            expectedSnapshot,
            context.TradingRulesEngine.LastTradingRules);
    }

    [Fact]
    public async Task ProcessAsync_LatestPriceFound_PassesRequestedPriceToRulesEngine()
    {
        var context = new OrderProcessorTestContext();
        OrderProcessor processor = context.CreateProcessor();

        await processor.ProcessAsync(context.Order);

        Assert.Equal(1, context.LatestPriceProvider.CallCount);
        Assert.Equal(context.Order.Symbol, context.LatestPriceProvider.LastSymbol);
        Assert.Same(
            context.MarketPrice,
            context.TradingRulesEngine.LastMarketPrice);
    }

    [Fact]
    public async Task ProcessAsync_MarketPriceUnavailable_ReturnsRejectedAndPersistsWithoutRulesEngine()
    {
        var context = new OrderProcessorTestContext();
        context.LatestPriceProvider.LatestPrice = null;
        OrderProcessor processor = context.CreateProcessor();

        TradeDecision decision = await processor.ProcessAsync(context.Order);

        Assert.Equal(OrderStatus.Rejected, decision.Status);
        RejectionReason reason = Assert.Single(decision.RejectionReasons);
        Assert.Equal("MarketPriceUnavailable", reason.Code);
        Assert.Equal(1, context.OrderIdRegistry.CallCount);
        Assert.Equal(context.Order.Id, context.OrderIdRegistry.LastOrderId);
        Assert.Equal(0, context.TradingRulesEngine.CallCount);
        Assert.Equal(1, context.OrderRepository.CallCount);
        Assert.Same(context.Order, context.OrderRepository.LastOrder);
        Assert.Same(decision, context.OrderRepository.LastDecision);
    }

    [Fact]
    public async Task ProcessAsync_DuplicateAndMarketPriceUnavailable_ReturnsBothReasonsInDeterministicOrder()
    {
        var context = new OrderProcessorTestContext();
        context.OrderIdRegistry.TryRegisterResult = false;
        context.LatestPriceProvider.LatestPrice = null;
        OrderProcessor processor = context.CreateProcessor();

        TradeDecision decision = await processor.ProcessAsync(context.Order);

        Assert.Equal(OrderStatus.Rejected, decision.Status);
        Assert.Equal(
            ["DuplicateOrderId", "MarketPriceUnavailable"],
            decision.RejectionReasons.Select(static reason => reason.Code));
        Assert.Equal(1, context.OrderIdRegistry.CallCount);
        Assert.Equal(0, context.TradingRulesEngine.CallCount);
        Assert.Equal(1, context.OrderRepository.CallCount);
        Assert.Same(decision, context.OrderRepository.LastDecision);
    }

    [Fact]
    public async Task ProcessAsync_PersistenceFails_PropagatesException()
    {
        var context = new OrderProcessorTestContext();
        var expectedException = new InvalidOperationException("Persistence failed.");
        context.OrderRepository.ExceptionToThrow = expectedException;
        OrderProcessor processor = context.CreateProcessor();

        InvalidOperationException actualException =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => processor.ProcessAsync(context.Order));

        Assert.Same(expectedException, actualException);
        Assert.Equal(1, context.OrderRepository.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_CancellationRequested_PropagatesCancellation()
    {
        var context = new OrderProcessorTestContext();
        context.OrderIdRegistry.ObserveCancellation = true;
        OrderProcessor processor = context.CreateProcessor();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => processor.ProcessAsync(
                context.Order,
                cancellationSource.Token));

        Assert.Equal(
            cancellationSource.Token,
            context.OrderIdRegistry.LastCancellationToken);
        Assert.Equal(0, context.TradingRulesEngine.CallCount);
        Assert.Equal(0, context.OrderRepository.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_NullOrder_ThrowsArgumentNullException()
    {
        var context = new OrderProcessorTestContext();
        OrderProcessor processor = context.CreateProcessor();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => processor.ProcessAsync(null!));

        Assert.Equal(0, context.TradingRulesProvider.ReadCount);
        Assert.Equal(0, context.OrderRepository.CallCount);
    }

    [Fact]
    public async Task ProcessAsync_RulesEngineThrowsProgrammingError_PropagatesException()
    {
        var context = new OrderProcessorTestContext();
        var expectedException = new ArgumentException("Invalid market context.");
        context.TradingRulesEngine.ExceptionToThrow = expectedException;
        OrderProcessor processor = context.CreateProcessor();

        ArgumentException actualException =
            await Assert.ThrowsAsync<ArgumentException>(
                () => processor.ProcessAsync(context.Order));

        Assert.Same(expectedException, actualException);
        Assert.Equal(0, context.OrderRepository.CallCount);
    }

    private static RejectionReason CreateReason(string code)
    {
        return new RejectionReason(code, $"{code} occurred.");
    }
}
