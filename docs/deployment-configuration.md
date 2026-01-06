# TravelBridge Deployment & Configuration

This document describes the configuration options and deployment guidelines for TravelBridge.

## Configuration Files

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "MariaDBConnection": "Server=<host>;Database=<db>;User=<user>;Password=<pass>"
  },
  "WebHotelierApi": {
    "BaseUrl": "https://api.webhotelier.net/v2",
    "Username": "<username>",
    "Password": "<password>"
  },
  "VivaApi": {
    "BaseUrl": "https://api.vivapayments.com",
    "AuthUrl": "https://accounts.vivapayments.com",
    "ClientId": "<client_id>",
    "ClientSecret": "<client_secret>",
    "SourceCode": "<source_code>",
    "SourceCodeTravelProject": "<alternate_source_code>"
  },
  "MapBoxApi": {
    "BaseUrl": "https://api.mapbox.com",
    "ApiKey": "<mapbox_api_key>"
  },
  "HereMapsApi": {
    "BaseUrl": "https://autocomplete.search.hereapi.com/v1",
    "ApiKey": "<here_api_key>"
  },
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": "587",
    "Username": "<smtp_user>",
    "Password": "<smtp_pass>",
    "From": "bookings@example.com"
  },
  "TestCard": {
    "CardNumber": "4111111111111111",
    "CardType": "Visa",
    "CardName": "Test User",
    "CardMonth": "12",
    "CardYear": "2030",
    "CardCVV": "123"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

## Environment-Specific Configuration

### Development (appsettings.Development.json)

```json
{
  "VivaApi": {
    "BaseUrl": "https://demo-api.vivapayments.com",
    "AuthUrl": "https://demo-accounts.vivapayments.com"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "VivaApi": {
    "BaseUrl": "https://api.vivapayments.com",
    "AuthUrl": "https://accounts.vivapayments.com"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

## Environment Variables

Configuration can be overridden via environment variables:

```bash
# Database
ConnectionStrings__MariaDBConnection="Server=...;Database=...;User=...;Password=..."

# WebHotelier
WebHotelierApi__BaseUrl="https://api.webhotelier.net/v2"
WebHotelierApi__Username="username"
WebHotelierApi__Password="password"

# Viva Wallet
VivaApi__BaseUrl="https://api.vivapayments.com"
VivaApi__ClientId="client_id"
VivaApi__ClientSecret="client_secret"
VivaApi__SourceCode="source_code"

# MapBox
MapBoxApi__ApiKey="pk.xxx..."

# SMTP
Smtp__Host="smtp.example.com"
Smtp__Port="587"
Smtp__Username="user"
Smtp__Password="pass"
Smtp__From="bookings@example.com"
```

## Database Configuration

### MariaDB Connection String

```
Server=<hostname>;
Database=travelbridge;
User=<username>;
Password=<password>;
Port=3306;
```

### Entity Framework Configuration

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.Parse("10.11.10-MariaDB"),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3, 
                maxRetryDelay: TimeSpan.FromSeconds(10), 
                errorNumbersToAdd: null);
        }
    ));
```

### Database Migrations

```bash
# Run migrations
dotnet ef database update -p TravelBridge.API

# Create new migration
dotnet ef migrations add MigrationName -p TravelBridge.API

# Generate SQL script
dotnet ef migrations script -p TravelBridge.API -o migration.sql
```

## Logging Configuration

### Serilog Setup

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/travelbridge-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 50 * 1024 * 1024,  // 50 MB
        rollOnFileSizeLimit: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
```

### Log Files

- **Location**: `logs/travelbridge-YYYY-MM-DD.log`
- **Retention**: 30 days
- **Max Size**: 50 MB per file (rolls over)

---

## Production Features

### 1. HTTP Retry Policies (Polly)

All external HTTP clients are configured with Polly retry policies for resilience:

| Client | Retries | Delays | Notes |
|--------|---------|--------|-------|
| WebHotelier | 3 | 200ms, 400ms, 800ms | Exponential backoff |
| MapBox | 3 | 200ms, 400ms, 800ms | Exponential backoff |
| HereMaps | 3 | 200ms, 400ms, 800ms | Exponential backoff |
| Viva (Payments) | 1 | 100ms | Fast-fail for payments |

```csharp
// Example retry policy
HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => 
            TimeSpan.FromMilliseconds(200 * Math.Pow(2, retryAttempt - 1)));
```

**Packages**: `Microsoft.Extensions.Http.Polly`, `Polly`

### 2. Health Checks

Health endpoint available at `/health` with MySQL database connectivity check:

```csharp
builder.Services.AddHealthChecks()
    .AddMySql(connectionString!, name: "database", tags: ["db", "mysql"]);

app.MapHealthChecks("/health");
```

**Response**: Returns healthy/unhealthy status based on database connectivity.

**Package**: `AspNetCore.HealthChecks.MySql`

### 3. Response Caching (IMemoryCache)

Hotel and room information is cached in memory to reduce API calls:

| Data | Cache Duration | Cache Key Pattern |
|------|---------------|-------------------|
| Hotel Info | 6 hours | `hotel_info_{hotelId}` |
| Room Info | 6 hours | `room_info_{hotelId}_{roomCode}` |

```csharp
// Cache check and set
if (_cache.TryGetValue(cacheKey, out WHHotelInfoResponse? cachedResult))
    return cachedResult;

var result = await _whClient.GetHotelInfoAsync(hotelId);
_cache.Set(cacheKey, result, TimeSpan.FromHours(6));
```

**Package**: `Microsoft.Extensions.Caching.Memory`

### 4. Rate Limiting

API rate limiting protects against abuse:

- **Limit**: 100 requests per minute per IP
- **Response**: HTTP 429 Too Many Requests when exceeded
- **Behavior**: Immediate rejection (no queuing)

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});
```

### 5. CORS Configuration

Environment-specific CORS policies:

**Development**: All origins allowed
```csharp
policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
```

**Production**: Restricted to known origins
```csharp
policy.WithOrigins(
    "https://my-diakopes.gr",
    "https://www.my-diakopes.gr",
    "https://travelproject.gr",
    "https://www.travelproject.gr")
    .AllowAnyMethod()
    .AllowAnyHeader();
```

### 6. Correlation ID Middleware

Request tracking for log correlation and debugging:

- **X-Session-Id**: Read from frontend request header (or auto-generated)
- **X-Request-Id**: Auto-generated per request
- Both returned in response headers
- Both enriched in Serilog log context

```csharp
// Get or generate session ID
var sessionId = context.Request.Headers["X-Session-Id"].FirstOrDefault() 
                ?? Guid.NewGuid().ToString("N")[..8];
var requestId = Guid.NewGuid().ToString("N")[..8];

// Set response headers
context.Response.Headers["X-Session-Id"] = sessionId;
context.Response.Headers["X-Request-Id"] = requestId;

// Enrich log context
using (LogContext.PushProperty("SessionId", sessionId))
using (LogContext.PushProperty("RequestId", requestId))
{
    // Request processing...
}
```

### 7. Response Compression

Gzip compression enabled for all HTTPS responses:

```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
```

### 8. Database Connection Retry

Entity Framework Core configured with automatic retry on transient failures:

```csharp
mySqlOptions.EnableRetryOnFailure(
    maxRetryCount: 3, 
    maxRetryDelay: TimeSpan.FromSeconds(10), 
    errorNumbersToAdd: null);
```

---

## Build & Deploy

### Build Commands

```bash
# Restore packages
dotnet restore

# Build
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./publish

# Run
dotnet TravelBridge.API.dll
```

### Docker Deployment

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TravelBridge.API.dll"]
```

```yaml
# docker-compose.yml
version: '3.8'
services:
  api:
    build: .
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__MariaDBConnection=Server=db;Database=travelbridge;User=root;Password=secret
    depends_on:
      - db

  db:
    image: mariadb:10.11
    environment:
      MYSQL_ROOT_PASSWORD: secret
      MYSQL_DATABASE: travelbridge
    volumes:
      - db_data:/var/lib/mysql

volumes:
  db_data:
```

## Security Considerations

### Secrets Management

**Development**: Use User Secrets
```bash
dotnet user-secrets init
dotnet user-secrets set "VivaApi:ClientSecret" "secret_value"
```

**Production**: Use environment variables or secret management service

### HTTPS

Configure HTTPS in production:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://+:80"
      },
      "Https": {
        "Url": "https://+:443",
        "Certificate": {
          "Path": "/path/to/cert.pfx",
          "Password": "cert_password"
        }
      }
    }
  }
}
```

## Monitoring Recommendations

### Application Insights (Azure)

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation());
```

## Performance Tuning

### Connection Pooling

MariaDB connection pooling is automatic. Configure:

```
Server=host;Database=db;User=user;Password=pass;
Pooling=true;
MinPoolSize=5;
MaxPoolSize=100;
ConnectionLifetime=300;
```

### HTTP Client Factory

HTTP clients use `IHttpClientFactory` for connection pooling:

```csharp
services.AddHttpClient("WebHotelierApi", client => { ... });
```

## Troubleshooting

### Common Issues

| Issue | Possible Cause | Solution |
|-------|---------------|----------|
| DB connection timeout | Network/firewall | Check connectivity, increase timeout |
| WebHotelier 401 | Invalid credentials | Verify username/password |
| Viva 403 | Invalid OAuth | Check client ID/secret |
| Email not sending | SMTP config | Verify host, port, credentials |
| HTTP 429 | Rate limit exceeded | Wait 1 minute or increase limit |

### Log Levels for Debugging

```csharp
// Temporarily enable detailed logging
.MinimumLevel.Debug()
.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
.MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Debug)
```

### Database Query Logging

```csharp
options.UseMySql(connectionString, serverVersion)
    .EnableSensitiveDataLogging()  // Shows parameter values
    .EnableDetailedErrors();        // More error details
```

## Backup & Recovery

### Database Backup

```bash
# MariaDB dump
mysqldump -u user -p travelbridge > backup.sql

# Restore
mysql -u user -p travelbridge < backup.sql
```

### Log Backup

Logs are stored in `logs/` directory. Archive before deletion:

```bash
# Archive logs older than 30 days
find logs/ -name "*.log" -mtime +30 -exec gzip {} \;
```

## Scaling Considerations

1. **Horizontal Scaling**: Deploy multiple API instances behind load balancer
2. **Database**: Consider read replicas for read-heavy operations
3. **Caching**: Upgrade to Redis for distributed caching across instances
4. **Rate Limiting**: Consider distributed rate limiting with Redis for multi-instance deployments
