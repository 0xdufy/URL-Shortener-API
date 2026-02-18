# URL Shortener API

## Overview
URL Shortener API is a .NET 8 ASP.NET Core Web API that creates short URLs, redirects using short codes, tracks click logs, supports daily stats, allows activate/deactivate, and soft delete.

## Prerequisites
- .NET 8 SDK
- SQL Server instance
- `dotnet-ef` tool

## Setup
1. Restore packages:
```bash
dotnet restore
```

2. Install EF tool (if not installed):
```bash
dotnet tool install --global dotnet-ef
```

3. Create migration:
```bash
cd UrlShortener.Api
dotnet ef migrations add InitialCreate --project ../UrlShortener.Infrastructure --startup-project .
```

4. Update database:
```bash
dotnet ef database update --project ../UrlShortener.Infrastructure --startup-project .
```

5. Run API:
```bash
dotnet run
```

## Endpoints
- `POST /api/v1/short-urls`
- `GET /r/{shortCode}`
- `GET /api/v1/short-urls/{shortCode}`
- `PATCH /api/v1/short-urls/{shortCode}/status`
- `DELETE /api/v1/short-urls/{shortCode}`
- `GET /api/v1/short-urls/{shortCode}/stats?fromUtc=&toUtc=`

## cURL Examples

### Create
```bash
curl -X POST "http://localhost:5000/api/v1/short-urls" \
  -H "Content-Type: application/json" \
  -d "{\"originalUrl\":\"https://example.com/very/long/url\",\"customAlias\":\"myAlias_01\",\"expiresAtUtc\":\"2026-03-01T00:00:00Z\"}"
```

### Redirect
```bash
curl -i "http://localhost:5000/r/myAlias_01"
```

### Get Details
```bash
curl "http://localhost:5000/api/v1/short-urls/myAlias_01"
```

### Update Status
```bash
curl -X PATCH "http://localhost:5000/api/v1/short-urls/myAlias_01/status" \
  -H "Content-Type: application/json" \
  -d "{\"isActive\":false}"
```

### Delete
```bash
curl -X DELETE "http://localhost:5000/api/v1/short-urls/myAlias_01"
```

### Stats
```bash
curl "http://localhost:5000/api/v1/short-urls/myAlias_01/stats?fromUtc=2026-02-01T00:00:00Z&toUtc=2026-02-18T00:00:00Z"
```
