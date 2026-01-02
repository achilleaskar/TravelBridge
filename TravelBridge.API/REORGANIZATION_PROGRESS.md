Yep — I opened **`TravelBridge-3rdTryFromScratch.zip`** and checked the solution structure + where the coupling still lives. Here’s what you have **right now**, what’s **already good**, what’s still **mixed**, and the **next steps** I’d do in the safest order.

---

## What’s true in your current branch (facts from the zip)

### ✅ Good separation already

* `TravelBridge.Contracts` **does not reference** any provider projects (no `ProjectReference`).
* `TravelBridge.Providers.WebHotelier` **does not reference** Contracts (only its own models).
* API does have a clear “boundary mapper”:

  * `TravelBridge.API/Helpers/Extensions/WHToContractsMapper.cs` maps `WH* → Contracts*`.

### ⚠️ Where the mixing still happens

1. **WebHotelier “provider logic” is still in API**, not in the provider project

   * Heavy HTTP + business logic is in:

     * `TravelBridge.API/Models/WebHotelier/WebHotelierPropertiesService.cs`
   * It uses:

     * DB models + repository + email sender
     * WH wire DTOs
     * Contracts DTOs
     * merge/cover/coupon logic

   Your provider project currently contains **mostly models** (`WH*`) and request containers (`WHAvailabilityRequest`, etc.), but **not the actual API client**.

2. **Geo projects exist but are empty**

* `TravelBridge.Geo.Mapbox` and `TravelBridge.Geo.HereMaps` have **no .cs files**, while Mapbox/Here services are still in API:

  * `TravelBridge.API/Services/ExternalServices/MapBoxService.cs`
  * `TravelBridge.API/Services/ExternalServices/HereMapsService.cs`

3. **API has dangerous global usings that make model leaking easy**

* `TravelBridge.API/GlobalUsings.cs` includes:

  * `global using TravelBridge.Providers.WebHotelier.Models.Hotel;`
  * `global using TravelBridge.Providers.WebHotelier.Models.Rate;`

  This is a “leak machine”: any random file can start using WH models without even noticing.

4. Viva is extracted (good), but still web-coupled

* `TravelBridge.Payments.Viva/VivaService.cs` uses `IHttpContextAccessor` to read Origin/Referer → payment provider depends on HTTP context.

---

## The best next steps (safe, incremental, no “big bang”)

### Step 1 — Stop future leaks (small but huge impact)

**Edit:** `TravelBridge.API/GlobalUsings.cs`

Remove these two lines:

* `global using TravelBridge.Providers.WebHotelier.Models.Hotel;`
* `global using TravelBridge.Providers.WebHotelier.Models.Rate;`

Then add explicit `using TravelBridge.Providers.WebHotelier...` only in:

* `WebHotelierPropertiesService.cs`
* `WHToContractsMapper.cs`
* any file that truly needs WH types

✅ Outcome: you prevent the “oops I used WH model in API response again” problem.

---

### Step 2 — Make Geo projects real (easy win)

Move these from API into their projects (keep namespaces unchanged at first if you want minimum edits):

**Mapbox**

* `TravelBridge.API/Services/ExternalServices/MapBoxService.cs` → `TravelBridge.Geo.Mapbox/...`
* `TravelBridge.API/Models/Apis/MapBoxApiOptions.cs` → `TravelBridge.Geo.Mapbox/...`
* `TravelBridge.API/Models/ExternalModels/MapBoxAutoCompleteResponse.cs` → `TravelBridge.Geo.Mapbox/...`

**Here**

* `HereMapsService.cs`, `HereApiOptions.cs`, `HereMapsAutoCompleteResponse.cs` → `TravelBridge.Geo.HereMaps/...`

Update `Program.cs` to register services from the new projects (same as today, just change the using + type locations).

✅ Outcome: Geo providers become “drop-in” like Viva already is.

---

### Step 3 — Turn WebHotelier project into a real provider (without touching DB/email yet)

Right now the “WebHotelier provider” is basically DTOs. The next safe move is:

#### 3A) Create a pure HTTP client inside `TravelBridge.Providers.WebHotelier`

Create a new class (example structure):

* `TravelBridge.Providers.WebHotelier/WebHotelierClient.cs`
* `TravelBridge.Providers.WebHotelier/Models/Apis/WebHotelierApiOptions.cs`
* `TravelBridge.Providers.WebHotelier/ServiceCollectionExtensions.cs`

**`WebHotelierApiOptions`** (move it out of API; it’s currently in `TravelBridge.API/Models/Apis/WebHotelierApiOptions.cs`)

**`AddWebHotelier(...)`** registers the named HttpClient + client class:

```csharp
namespace TravelBridge.Providers.WebHotelier;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebHotelier(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<WebHotelierApiOptions>(config.GetSection("WebHotelierApi"));

        services.AddHttpClient("WebHotelierApi", (sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<WebHotelierApiOptions>>().Value;
            client.BaseAddress = new Uri(opt.BaseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("Accept-Language", "el");

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{opt.Username}:{opt.Password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        });

        services.AddScoped<WebHotelierClient>();
        return services;
    }
}
```

#### 3B) Make API service depend on `WebHotelierClient` (not HttpClientFactory)

Change `WebHotelierPropertiesService` ctor from:

* `(IHttpClientFactory httpClientFactory, SmtpEmailSender mailSender)`
  to:
* `(WebHotelierClient whClient, SmtpEmailSender mailSender)`

Then in that file, replace the raw `_httpClient.GetAsync(...) + JsonSerializer.Deserialize<...>` blocks with `whClient.*` calls.

✅ Outcome:

* All WebHotelier HTTP endpoints + wire models now live together in the provider project
* API service becomes orchestration only (DB/email/merging/coupon logic can stay for now)

This is the single biggest “you won’t explode later” step.

---

### Step 4 — Move business logic OUT of response DTOs (optional but recommended)

You have DTOs with behavior (example):

* `TravelBridge.API/Contracts/CheckoutResponse.cs` has `MergePayments(...)`
* `PluginSearchResponse.cs` has `CoverRequest(...)`

This makes refactoring painful because moving “models” drags logic and dependencies.

Create a service in API (or Application later):

* `CheckoutComposer`
* `AvailabilityPostProcessor`

and move these methods there.

✅ Outcome: contracts become simple “bags of data”, easier to share/move.

---

### Step 5 — Only after that: introduce ports for “more providers”

Right now API is still effectively “WebHotelier-first”.

Once Step 3 is done, you can introduce in `TravelBridge.Application`:

* `IHotelProvider`
* `IPaymentProvider`

…and make:

* WebHotelier provider implement `IHotelProvider`
* Viva implement `IPaymentProvider`

For your “own hotels” feature, you add:

* `TravelBridge.Providers.InternalHotels` (DB-backed) implementing the same interface

That’s when “new provider” becomes **add a project + register it**, not “touch 12 endpoints”.

---

## One immediate improvement I strongly recommend (because it causes pain later)

### Don’t keep WH DTOs `public` unless you must

Right now `WHPartyItem`, `WHHotelData`, etc. are `public`.
If you keep API consuming them, they must be public, fine.

But once you add `WebHotelierClient` and push mapping into provider or Application, you can make them:

* `internal` (best)
* or at least keep them under a `/Wire/` folder to make it obvious they must not leak

---

## If you want: I can give you a concrete “next 2 commits” checklist

Based on your code, the most valuable next two commits are:

1. **Remove provider global usings + make Geo projects real**
2. **Add WebHotelierClient + AddWebHotelier + refactor WebHotelierPropertiesService to use the client**

If you say “go”, I’ll write those steps as a literal checklist with the exact files to edit and exactly what to paste where (minimal churn, build stays green).
