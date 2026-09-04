# Local development

The Docker Compose setup runs PostgreSQL 17 only. Its `postgres` / `postgres`
credentials and the connection string in `appsettings.json` are for local
development only.

Start PostgreSQL:

```powershell
docker compose up -d
```

Ordinary tests do not require PostgreSQL. To opt into the real PostgreSQL tests,
set a server connection string; the fixture always creates and uses the isolated
`pricing_trading_tests` database rather than the development database:

```powershell
$env:PRICING_TRADING_TEST_DB = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"
dotnet test
Remove-Item Env:PRICING_TRADING_TEST_DB
```

`ConnectionStrings__TradingDatabase` can override the application's configured
`TradingDatabase` connection string.

Stop PostgreSQL when finished:

```powershell
docker compose down
```
