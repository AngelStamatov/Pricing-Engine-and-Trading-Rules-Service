# Pricing Engine and Trading Rules Service

## Overview

This .NET 10 service is a pragmatic modular monolith for simulated pricing and order validation. It continuously produces prices for ten FX symbols, sends ticks through a bounded in-process pipeline, calculates CurrentMarketPrice (mid), Spread, and SpreadPercent, and maintains concurrent latest-price state.

The service accepts manual Limit orders, evaluates runtime-configurable trading rules, and creates spread-based automatic orders. PostgreSQL stores orders, final decisions, active rules, and recoverable latest-price snapshots. REST endpoints expose order submission, rules, current prices, and paginated history.

Manual and auto-generated orders share the same final `OrderProcessor` validation and persistence path.

## Architecture

Production code is split into four projects:

- `PricingAndTrading.Domain`: immutable models, invariants, and pure calculations; no EF Core or ASP.NET Core dependencies.
- `PricingAndTrading.Application`: orchestration, rule evaluation, auto-trading, interfaces, and history read models.
- `PricingAndTrading.Infrastructure`: channel feed, workers, runtime state, EF Core repositories, and PostgreSQL.
- `PricingAndTrading.Api`: HTTP contracts, validation, controllers, serialization, exception handling, and composition.

Tests live in `PricingAndTrading.UnitTests` and `PricingAndTrading.IntegrationTests`. The result is cleanly separated application code in one deployable modular monolith, not a claim of strict textbook Clean Architecture.

```text
Price producers (10 symbols)
    -> bounded Channel<PriceTick>
    -> PriceProcessor
         +--> LatestPriceStore
         +--> AutoTradingEngine --> OrderProcessor
                                      +--> duplicate registration
                                      +--> TradingRulesEngine
                                      +--> order/decision persistence

Manual API orders -----------------> OrderProcessor
```

At startup, EF Core migrations run, persisted rules are loaded (or configured defaults are seeded), and persisted latest prices are restored before normal background processing.

## How to Run

Prerequisites:

- .NET 10 SDK
- Docker Desktop or Docker Engine with Docker Compose
- PostgreSQL supplied by `docker-compose.yml`

From the repository root:

```shell
docker compose up -d
dotnet run --project src/PricingAndTrading.Api
```

Without an ASP.NET Core URL override, the API listens at `http://localhost:5000`. Stop it with `Ctrl+C`, then stop PostgreSQL:

```shell
docker compose down
```

Override the database through `ConnectionStrings__TradingDatabase`. The committed `postgres` / `postgres` credentials and `pricing_trading` database are local-development settings only and are not production-ready secrets.

## API

Enums are serialized as strings (`Buy`, `Limit`, `Api`, `Accepted`, `Rejected`).

| Method and route | Semantics |
| --- | --- |
| `POST /api/orders` | Validates and submits a manual Limit order through `IOrderProcessor`. |
| `GET /api/trading-rules` | Returns the current in-memory rules snapshot. |
| `PUT /api/trading-rules` | Persists and then replaces the complete rules snapshot. |
| `GET /api/trades/history` | Returns filtered, newest-first paginated history. |
| `GET /api/prices/{symbol}` | Returns the latest in-memory price, or HTTP 404. |
| `GET /api/orders/{symbol}/history` | Returns paginated history for one symbol. |

History filters are `symbol`, `status`, `source`, `from`, `to`, `page`, and `pageSize`. Page defaults to 1, page size defaults to 50, and the maximum page size is 100.

A structurally valid order rejected by business rules returns HTTP 200 with `status: "Rejected"` and structured `rejectionReasons`. HTTP 400 is for invalid API input or configuration. Unexpected infrastructure/system failures become HTTP 5xx and are not business rejections.

```json
{
  "orderId": "11111111-1111-1111-1111-111111111111",
  "symbol": "EURUSD",
  "side": "Buy",
  "type": "Limit",
  "price": 1.085,
  "quantity": 100
}
```

See [`PricingAndTrading.http`](PricingAndTrading.http) for all endpoint examples.

## Design Decisions

### .NET 10

.NET 10 was selected because the requirement permits .NET 9+ and .NET 10 is the current LTS target used by the implementation. Nullable reference types and implicit usings are enabled.

### Modular monolith

One process keeps deployment and operational complexity proportionate to the exercise. Explicit project boundaries allow component replacement without premature microservices.

### Bounded Channel with backpressure

The price feed is a bounded `Channel<PriceTick>` with capacity 1024, multiple producers, one consumer, and `FullMode.Wait`. Ticks are not intentionally dropped because auto-trading must evaluate every processed price update. Producers wait when downstream work slows.

### Concurrent producers, single price consumer

Ten symbol producers run concurrently while one consumer gives simple, deterministic previous/current semantics. This is sufficient for the MVP but has lower theoretical throughput than partitioned processing.

### Latest-price in-memory state

`LatestPriceStore` uses `ConcurrentDictionary`. An update atomically replaces the current symbol value and returns the previous value, avoiding a separate read/update race during comparison.

### Price persistence strategy

Every tick is not persisted. Hot state stays in memory and latest snapshots are saved approximately once per second. This reduces write amplification and supports restart recovery, but abrupt failure can lose roughly one persistence interval. The read/update/add repository is deliberately scoped to one instance, one writer, and about ten symbols.

### TradingRules runtime snapshot

`TradingRules` is an immutable Domain snapshot read from memory on hot paths. Updates are serialized within one process, persisted first, and only then atomically replace runtime state. Cross-instance coordination is outside this MVP.

### Trading rules

State-independent rules are synchronous and I/O-free. Duplicate validation is stateful, so it remains at the `OrderProcessor` boundary where the final `TradeDecision` is assembled.

### Duplicate registration

Targeted parameterized PostgreSQL SQL is retained:

```sql
INSERT INTO "OrderIdRegistrations" ("OrderId", "RegisteredAt")
VALUES (...)
ON CONFLICT ("OrderId") DO NOTHING
```

This atomic primitive avoids a SELECT-then-INSERT race and exception-driven normal duplicate handling.

### Separate PersistenceId and business OrderId

`Orders.PersistenceId` uniquely identifies each stored submission. `Orders.OrderId` is the client/business ID and is intentionally non-unique so repeated submissions can be recorded when duplicate rejection is disabled. `OrderIdRegistrations.OrderId` carries the uniqueness constraint for duplicate tracking.

### Auto-order sizing

The intentionally simple sizing formula is:

```text
targetNotional     = MaximumNotionalAmount * 10%
quantityByNotional = targetNotional / generatedPrice
quantityCap        = MaximumQuantity * 10%
quantity           = Min(quantityByNotional, quantityCap)
```

### Auto-trading price

The fixed 0.03% adjustment is:

```text
Sell: AskPrice - AskPrice * 0.0003
Buy:  BidPrice + BidPrice * 0.0003
```

These formulas are exercise policy, not production execution logic.

## Testing

`PricingAndTrading.UnitTests` covers Domain behavior, pure rules, orchestration, auto-trading, pricing, and serialized rules updates. `PricingAndTrading.IntegrationTests` covers runtime state, workers, DI, EF mappings, persistence boundaries, controllers, and history.

Run ordinary tests without Docker:

```shell
dotnet test
```

Real PostgreSQL tests opt in through `PRICING_TRADING_TEST_DB` and skip when absent. The fixture creates, cleans, and drops the isolated `pricing_trading_tests` database.

```shell
docker compose up -d
```

```text
# PowerShell
$env:PRICING_TRADING_TEST_DB = "Host=localhost;Port=5432;Database=pricing_trading_tests;Username=postgres;Password=postgres"
dotnet test

# Bash
export PRICING_TRADING_TEST_DB='Host=localhost;Port=5432;Database=pricing_trading_tests;Username=postgres;Password=postgres'
dotnet test
```

Live-provider coverage includes migrations, order/reason persistence, repeated business IDs, concurrent atomic registration, rules and PostgreSQL `text[]` round-trips, latest prices, and history filtering/pagination.

## AI Usage Transparency

The candidate made the architectural and behavioral decisions and reviewed the generated implementation incrementally. ChatGPT and Codex assisted with design discussion and alternatives, scoped implementation boilerplate, test scaffolding, concurrency and edge-case identification, implementation review, and documentation. Changes were reviewed after each step rather than accepting one generated solution wholesale.

Suggestions were challenged, refined, or overridden. Examples:

1. `TradingRulesEngine` was refactored from returning final `TradeDecision` to `TradeValidationResult`, because duplicate validation is stateful and final decisions belong in `OrderProcessor`.
2. Duplicate IDs are always registered; the runtime toggle controls rejection only.
3. Producers were changed from concrete `ChannelPriceFeed` to `IPriceTickPublisher`.
4. PostgreSQL `ON CONFLICT` was retained over pure EF Core for atomic registration.
5. Latest-price persistence stayed a simple single-instance snapshot instead of adding premature distributed/bulk infrastructure.

No manual-versus-AI authorship percentage is claimed; AI supported design review as well as implementation work.

## Known Limitations & Future Improvements

### Order registration vs final persistence

Registration and final persistence are separate database operations. If registration succeeds and persistence fails, an orphan registration can remain. A redesigned transactional persistence boundary could combine them.

### Single-instance assumptions

In-memory prices, runtime rules, the update `SemaphoreSlim`, and latest-price read/update/add persistence target one instance. Multi-instance deployment needs coordinated/distributed state or stronger database-level atomic operations.

### Rules snapshot timing

`PriceProcessor` reads rules for auto-order generation; `OrderProcessor` reads again for final validation. An intervening update can mean generation under one snapshot and validation under a newer one. This is accepted for the MVP.

### Single price consumer

`PriceProcessor` awaits auto-order processing. Slow persistence can block the consumer and apply channel backpressure. Ticks are not intentionally dropped, but sustained throughput is limited. A future design could partition by symbol or add another bounded stage while preserving per-symbol order.

### Latest price durability

Periodic persistence can lose up to roughly one interval of latest-price state on abrupt shutdown.

### Database migrations at startup

Startup migration is convenient locally. Multi-instance production would normally use a separate controlled deployment step.

### Security

Authentication and authorization were not required and are not implemented. Development credentials must not be reused in production.

### Pagination

History uses offset pagination; keyset/cursor pagination scales better for very large histories.

## Eventual C++ Migration

### 1. Pricing hot path / transport

The managed path uses `PriceTick` / `MarketPrice`, `Channel<T>`, and async producers/consumer. Native code could use compact POD structures, explicit ownership, cache-aware layouts, and a ring buffer or low-contention queue. `IPriceFeed`, `IPriceTickPublisher`, and `IPriceProcessor` isolate this change.

### 2. Trading-rule and auto-trading computation

Pure synchronous calculation is separated from EF/API concerns, making it a native candidate only if profiling finds a bottleneck. C++ needs stable transfer contracts, explicit decimal precision, a result/error ABI, and managed/native boundary cost analysis. It does not automatically improve this MVP.

### 3. Managed/native integration boundary

Performance-sensitive parts could use a native shared library via P/Invoke/source-generated interop, or a separate process with IPC for stronger isolation. ASP.NET Core, EF Core/PostgreSQL, configuration, and CRUD/history would likely remain in .NET because moving them offers little value.
