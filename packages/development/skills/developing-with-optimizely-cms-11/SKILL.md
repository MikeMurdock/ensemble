---
name: developing-with-optimizely-cms-11
description: Maintenance of Optimizely CMS 11 / Episerver CMS 11 and Optimizely Commerce 13 / Episerver Commerce 13 applications, including Search & Navigation (Find). Content types, IContentRepository, initialization modules, scheduled jobs, MVC rendering, the Commerce catalog/cart/order/pricing model, and Find queries. Use when debugging, modifying, or migrating EXISTING Optimizely/Episerver CMS 11 or Commerce 13 sites. NOT for greenfield sites or for Optimizely CMS 12+/Commerce 14+ (which run on modern .NET).
---

# Optimizely / Episerver CMS 11 & Commerce 13 Skill

Maintenance patterns for the **Optimizely/Episerver layer** of CMS 11 and Commerce 13 applications, plus **Search & Navigation (Find)**. Covers the content model, content repositories, initialization modules, scheduled jobs, MVC rendering, the Commerce catalog/cart/order/pricing system, and Find queries.

> **Naming:** Episerver was rebranded **Optimizely** (c. 2020–2021). At these versions the rename is **branding only** — the NuGet packages and namespaces are still **`EPiServer.*`** (Commerce internals are `Mediachase.*`). Documentation/marketing says "Optimizely"; the code says "EPiServer". The package/namespace rename to `Optimizely.*` happened later, at **CMS 12 / Commerce 14** (which target modern .NET and are out of scope here). Throughout this skill, "Optimizely CMS 11" and "Episerver CMS 11" mean the same product; code uses `EPiServer.*`.

> **This is a legacy-maintenance skill, not a greenfield one.** Use it to *understand, debug, modify, and migrate existing* CMS 11 / Commerce 13 sites. New sites should target current Optimizely on modern .NET.

**Progressive Disclosure**: This file is the quick reference. For comprehensive guides and the full API surface, see [REFERENCE.md](REFERENCE.md).

## Table of Contents

1. [When to Use](#when-to-use)
2. [Platform Layer (verify, don't assume)](#platform-layer-verify-dont-assume)
3. [Quick Start](#quick-start)
4. [Content Types](#content-types)
5. [Content Access (Repositories & Loaders)](#content-access-repositories--loaders)
6. [Initialization Modules](#initialization-modules)
7. [Dependency Injection (StructureMap)](#dependency-injection-structuremap)
8. [MVC Rendering](#mvc-rendering)
9. [Scheduled Jobs](#scheduled-jobs)
10. [Commerce 13](#commerce-13)
11. [Search & Navigation (Find)](#search--navigation-find)
12. [Legacy Debugging & Gotchas](#legacy-debugging--gotchas)
13. [Anti-Patterns](#anti-patterns)
14. [Authentication (Out of Scope)](#authentication-out-of-scope)
15. [See Also](#see-also)

---

## When to Use

Loaded when a repository shows the **Optimizely/Episerver CMS 11 fingerprint**:

- `packages.config` with `EPiServer.CMS*` / `EPiServer.Framework*` at **11.x** (and optionally `EPiServer.Commerce*` at **13.x**, `EPiServer.Find*` at **13.x**)
- Content classes with `[ContentType]`, deriving from `PageData` / `BlockData` / `MediaData`
- `IInitializableModule` / `IConfigurableModule` classes; `[ScheduledPlugIn]` jobs
- `IContentRepository` / `IContentLoader` usage; `Html.PropertyFor` in `.cshtml`
- Commerce: `ProductContent` / `VariationContent` / `NodeContent`, `IOrderRepository`, `Mediachase.*`

**Scope (in):** the Optimizely/Episerver API layer for CMS 11 and Commerce 13 — content modeling, content access, initialization, scheduled jobs, MVC/Razor rendering of content, edit/admin integration, the Commerce catalog/market/pricing/cart/order model, and Search & Navigation (Find) querying and indexing.

**Scope (out):**
- **The Microsoft platform underneath** (project system, `web.config`, EF6, OWIN, binding redirects, MSTest, IIS) — that is the **`developing-with-dotnet-4x`** skill's domain. See the next section; load both when the host is .NET Framework 4.x.
- **Optimizely CMS 12+ / Commerce 14+** and current Optimizely SaaS — different packages (`Optimizely.*`), modern .NET hosting, built-in DI. Out of scope; relevant only as a migration target.
- **Identity/authentication mechanisms** — see [Authentication (Out of Scope)](#authentication-out-of-scope).

---

## Platform Layer (verify, don't assume)

This skill covers the **Optimizely/Episerver layer only**. CMS 11 and Commerce 13 are the **last versions that target .NET Framework 4.x**, so a project genuinely on these versions is *very likely* on 4.x — but **confirm rather than assume**: check `<TargetFrameworkVersion>` in the project's `.csproj` (or the `targetFramework` moniker in `packages.config`).

- **If it's `v4.x`** (e.g. `v4.6.1`–`v4.8`): also load **`developing-with-dotnet-4x`** for the host platform — the old-style project system, `web.config` and binding redirects, EF6, OWIN/Katana startup, MSTest, and IIS-era debugging. This skill builds *on top of* that platform layer and does not repeat it.

Detect quickly:

```bash
# CMS/Commerce/Find versions (expect EPiServer.* 11.x / 13.x)
grep -rhE 'id="EPiServer\.(CMS|Framework|Commerce|Find)' --include="packages.config" . | sort -u

# Host framework — drives whether to also load developing-with-dotnet-4x
grep -rh "TargetFrameworkVersion" --include="*.csproj" .
```

---

## Quick Start

### A content type (page)

```csharp
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace Acme.Web.Models.Pages
{
    [ContentType(
        DisplayName = "Standard Page",
        GUID = "0c19c5e6-1f3a-4f2b-9a1d-000000000001",   // unique per content type
        Description = "A general-purpose content page.")]
    public class StandardPage : PageData
    {
        [Display(Name = "Heading", Order = 10)]
        [CultureSpecific]
        public virtual string Heading { get; set; }

        [Display(Name = "Main body", Order = 20)]
        [CultureSpecific]
        public virtual XhtmlString MainBody { get; set; }

        [Display(Name = "Main content area", Order = 30)]
        public virtual ContentArea MainContentArea { get; set; }
    }
}
```

### Read content

```csharp
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

public class ContentService
{
    private readonly IContentLoader _contentLoader;
    public ContentService(IContentLoader contentLoader) => _contentLoader = contentLoader;

    public StandardPage GetPage(ContentReference link) =>
        _contentLoader.Get<StandardPage>(link);

    public IEnumerable<StandardPage> GetChildren(ContentReference parent) =>
        _contentLoader.GetChildren<StandardPage>(parent);
}
```

> **More**: full content-type, controller, job, Commerce, and Find scaffolds in [templates/](templates/); end-to-end slices in [examples/](examples/).

---

## Content Types

Content types are POCOs decorated with `[ContentType]`, deriving from a base:

| Base type | Use |
|-----------|-----|
| `PageData` | Routable pages (have a URL) |
| `BlockData` | Reusable content blocks (composed into pages, no own URL) |
| `MediaData` / `ImageData` / `VideoData` | Media assets |
| `ProductContent` / `VariationContent` / `NodeContent` (Commerce) | Catalog entries — see [Commerce 13](#commerce-13) |

Properties are `public virtual` (Optimizely proxies them) with attributes:

```csharp
[ContentType(DisplayName = "Hero Block", GUID = "…", GroupName = "Acme")]
public class HeroBlock : BlockData
{
    [Display(Name = "Title", Order = 10), CultureSpecific, Required]
    public virtual string Title { get; set; }

    [Display(Name = "Image", Order = 20), UIHint(UIHint.Image)]
    public virtual ContentReference Image { get; set; }

    [Display(Name = "Call to action", Order = 30)]
    public virtual Url CtaLink { get; set; }
}
```

Common property types: `string`, `XhtmlString` (rich text), `ContentReference` / `PageReference`, `ContentArea` (composable), `Url`, `LinkItemCollection`, `bool`, `int`, `DateTime`. Common attributes: `[CultureSpecific]` (per-language value), `[Required]`, `[UIHint(...)]` (e.g. `Image`, `Textarea`), `[Searchable]`, `[Display(GroupName=…, Order=…)]`.

---

## Content Access (Repositories & Loaders)

```csharp
// Read-only access — prefer IContentLoader for reads.
IContentLoader loader;
var page = loader.Get<StandardPage>(contentLink);
var kids = loader.GetChildren<PageData>(contentLink);
loader.TryGet<StandardPage>(contentLink, out var maybePage);

// Mutations — IContentRepository (read + write).
IContentRepository repo;
var clone = (StandardPage)page.CreateWritableClone();   // never mutate the cached instance
clone.Heading = "Updated";
repo.Save(clone, EPiServer.DataAccess.SaveAction.Publish, EPiServer.Security.AccessLevel.NoAccess);

// Create new content under a parent.
var draft = repo.GetDefault<StandardPage>(parentLink);
draft.Heading = "New";
repo.Save(draft, EPiServer.DataAccess.SaveAction.Publish);
```

> **Cardinal rule:** content returned from a loader is **read-only and shared from cache** — never mutate it. Always `CreateWritableClone()` (or `GetDefault`) before changing and `Save`.

---

## Initialization Modules

Startup logic runs in modules discovered by Optimizely's initialization system (not `Global.asax`):

```csharp
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

[InitializableModule]
[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]   // run order via dependencies
public class CustomInitialization : IInitializableModule
{
    public void Initialize(InitializationEngine context)
    {
        var events = context.Locate.Advanced.GetInstance<IContentEvents>();
        events.PublishedContent += OnPublished;
    }

    public void Uninitialize(InitializationEngine context)
    {
        var events = context.Locate.Advanced.GetInstance<IContentEvents>();
        events.PublishedContent -= OnPublished;
    }

    private void OnPublished(object sender, EPiServer.ContentEventArgs e) { /* … */ }
}
```

- `IInitializableModule` — `Initialize` / `Uninitialize`. `[ModuleDependency(typeof(X))]` orders modules.
- `IConfigurableModule` — adds `ConfigureContainer(ServiceConfigurationContext)` for DI registration *before* the container is built. See next section.

---

## Dependency Injection (StructureMap)

CMS 11 uses **StructureMap** behind `IServiceLocator`. Register in an `IConfigurableModule`:

```csharp
[InitializableModule]
public class DependencyResolverInitialization : IConfigurableModule
{
    public void ConfigureContainer(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IProductService, ProductService>();
        context.Services.AddSingleton<IPriceCalculator, PriceCalculator>();
    }

    public void Initialize(InitializationEngine context) { }
    public void Uninitialize(InitializationEngine context) { }
}
```

- **Inject** via constructor (preferred), or the `Injected<T>` property wrapper, or — as a last resort — `ServiceLocator.Current.GetInstance<T>()`.
- `[ServiceConfiguration(typeof(IFoo))]` on a class auto-registers it.

> **Note:** this is *Optimizely's* DI (StructureMap-backed), distinct from any container in the host app. CMS 12 replaces it with built-in ASP.NET Core DI — a migration concern.

---

## MVC Rendering

CMS 11 renders content through ASP.NET **MVC 5** using typed controllers (this is where this skill meets `developing-with-dotnet-4x`'s MVC coverage):

```csharp
using EPiServer.Web.Mvc;

public class StandardPageController : PageController<StandardPage>
{
    public ActionResult Index(StandardPage currentPage)
    {
        return View(currentPage);
    }
}

// Blocks render through BlockController<T>.
public class HeroBlockController : BlockController<HeroBlock>
{
    public override ActionResult Index(HeroBlock currentBlock) => PartialView(currentBlock);
}
```

In Razor views, render properties through `Html.PropertyFor` so on-page editing works:

```cshtml
@model Acme.Web.Models.Pages.StandardPage
<h1>@Html.PropertyFor(m => m.Heading)</h1>
<div>@Html.PropertyFor(m => m.MainBody)</div>
@Html.PropertyFor(m => m.MainContentArea)
```

> `Html.PropertyFor` emits edit metadata in edit mode; outputting `@Model.Heading` directly breaks the editor experience.

---

## Scheduled Jobs

```csharp
using EPiServer.PlugIn;
using EPiServer.Scheduler;

[ScheduledPlugIn(
    DisplayName = "Acme Nightly Sync",
    GUID = "0c19c5e6-1f3a-4f2b-9a1d-000000000010",
    IntervalLength = 24, IntervalType = ScheduledIntervalType.Hours)]
public class NightlySyncJob : ScheduledJobBase
{
    private bool _stopped;
    public NightlySyncJob() { IsStoppable = true; }

    public override void Stop() => _stopped = true;

    public override string Execute()
    {
        OnStatusChanged("Starting…");
        // … work, checking _stopped periodically …
        return "Done: processed N items.";
    }
}
```

---

## Commerce 13

`EPiServer.Commerce` 13 builds catalog content on the CMS content model, with an order system layered over the legacy `Mediachase.*` foundation.

### Catalog content

```csharp
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Commerce.Catalog.DataAnnotations;

[CatalogContentType(GUID = "…", DisplayName = "Product")]
public class AcmeProduct : ProductContent      // groups variations
{
    [Display(Name = "Description", Order = 10), CultureSpecific]
    public virtual XhtmlString Description { get; set; }
}

[CatalogContentType(GUID = "…", DisplayName = "Variation")]
public class AcmeVariation : VariationContent  // the buyable SKU
{
    [Display(Name = "Color", Order = 10)]
    public virtual string Color { get; set; }
}
// NodeContent = category; ProductContent groups VariationContent (the purchasable SKUs).
```

### Cart & orders

```csharp
using EPiServer.Commerce.Order;

public class CartService
{
    private readonly IOrderRepository _orderRepository;
    public CartService(IOrderRepository orderRepository) => _orderRepository = orderRepository;

    public ICart GetOrCreateCart(CustomerContext customer) =>
        _orderRepository.LoadOrCreateCart<ICart>(customer.CurrentContactId, "Default");

    public void AddItem(ICart cart, string code, decimal qty)
    {
        var form = cart.GetFirstForm();
        var shipment = cart.GetFirstShipment();
        var line = cart.CreateLineItem(code);   // IOrderGroupFactory under the hood
        line.Quantity = qty;
        shipment.LineItems.Add(line);
        _orderRepository.Save(cart);
    }

    public IPurchaseOrder Checkout(ICart cart)
    {
        var orderRef = _orderRepository.SaveAsPurchaseOrder(cart);
        return _orderRepository.Load<IPurchaseOrder>(orderRef.OrderGroupId);
    }
}
```

### Pricing

```csharp
using EPiServer.Commerce.Pricing;

public class PriceLookup
{
    private readonly IPriceDetailService _priceDetailService;
    public PriceLookup(IPriceDetailService priceDetailService) => _priceDetailService = priceDetailService;

    public IEnumerable<IPriceDetailValue> ListPrices(CatalogContentBase entry) =>
        _priceDetailService.List(entry.ContentLink);
}
```

Key abstractions: `IOrderRepository` (load/save carts & orders), `ICart` / `IPurchaseOrder` / `IOrderForm` / `IShipment` / `ILineItem`, `IPriceService` / `IPriceDetailService`, `IMarketService` (markets/currencies), `IPromotionEngine`. The older `Mediachase.Commerce.*` types (Catalog, Customers, Orders, Pricing) underlie the abstractions and surface in legacy code.

---

## Search & Navigation (Find)

`EPiServer.Find` 13 is the search engine — a typed query API over an index that auto-updates as content is published.

```csharp
using EPiServer.Find;
using EPiServer.Find.Cms;

public class SearchService
{
    private readonly IClient _findClient;
    public SearchService(IClient findClient) => _findClient = findClient;   // or SearchClient.Instance

    public IEnumerable<StandardPage> Search(string term)
    {
        return _findClient.Search<StandardPage>()
            .For(term)                                   // free-text
            .Filter(p => p.MainBody.Match(term))         // typed filters via FilterBuilder
            .FilterForVisitor()                          // access + publication filters
            .Skip(0).Take(10)
            .GetContentResult();                         // hydrate IContent (CMS)
    }
}
```

- Chain shape: `.Search<T>()` → `.For(text)` / `.Filter(expr)` → `.Skip/.Take` → `.GetResult()` / `.GetContentResult()`.
- `FilterBuilder` builds typed boolean filters; `.FilterForVisitor()` applies access + publish-status filtering for front-end queries.
- **Distinct from** the older built-in `EPiServer.Search` (Lucene-based); a CMS 11 site may carry both. This skill features **Find**; treat `EPiServer.Search` as legacy unless a project specifically uses it.

---

## Legacy Debugging & Gotchas

| Symptom | Cause | Fix |
|---------|-------|-----|
| Edited content "reverts" / changes lost | Mutated a cache-shared `IContent` instance | `CreateWritableClone()` before edit; `Save` the clone |
| Property always null in code, set in edit UI | Property not `public virtual` (no proxy) | Make it `public virtual`; rebuild |
| New content type not appearing | Type not synced / duplicate `GUID` | Check the content-type sync on startup; ensure GUID uniqueness |
| `On-page editing not working` for a field | Rendered with `@Model.X` not `Html.PropertyFor` | Use `Html.PropertyFor(m => m.X)` |
| Init module never runs / runs too early | Missing/incorrect `[ModuleDependency]` ordering | Order via dependencies; don't assume registration order |
| Find returns stale/missing results | Index not updated, or visitor filters omitted | Re-index; add `.FilterForVisitor()` for front-end queries |
| Commerce price/order null | Wrong market/currency, or cart not saved | Resolve via `IMarketService`; `IOrderRepository.Save` after mutation |
| `ServiceLocator` throws at startup | Resolved a service before container built | Register in `IConfigurableModule.ConfigureContainer`; resolve in `Initialize` |
| Assembly load / version errors at boot | Binding redirects (platform concern) | See `developing-with-dotnet-4x` — binding redirects |

> The host-platform failure classes (binding redirects, sync-over-async deadlocks, web.config transforms) live in **`developing-with-dotnet-4x`**. The table above is the Optimizely-layer subset.

---

## Anti-Patterns

| Anti-Pattern | Problem | Fix |
|--------------|---------|-----|
| Mutating loader-returned `IContent` | Corrupts the shared cache | `CreateWritableClone()` then `Save` |
| Non-virtual content properties | Optimizely can't proxy → values don't persist | `public virtual` |
| `ServiceLocator.Current` everywhere | Hidden deps, hard to test | Constructor injection; `ServiceLocator` only at composition edges |
| Outputting content with `@Model.X` | Breaks on-page editing | `Html.PropertyFor` |
| Querying content with raw DB/SQL | Bypasses cache, access rights, publish status | `IContentLoader` / Find with `FilterForVisitor` |
| Reusing one `GUID` across content types | Sync conflicts, missing types | Unique GUID per type |
| Treating CMS 12 docs as applicable | Different packages/DI/hosting | Use CMS 11-era (`EPiServer.*`) patterns |

---

## Authentication (Out of Scope)

This skill is **platform/content-focused and auth-agnostic**. A CMS 11 site may authenticate editors and visitors via ASP.NET Identity (the `EPiServer.CMS.UI.AspNetIdentity` package is common), OWIN cookie/OAuth middleware, an external OIDC provider, or another mechanism. The skill notes only where identity *integrates* with Optimizely (e.g. virtual roles, access rights on content); the identity mechanism itself is out of scope — bring your own. OWIN middleware ordering (where many auth integrations live) is covered by `developing-with-dotnet-4x`.

---

## See Also

- **[REFERENCE.md](REFERENCE.md)** — Comprehensive guide: full content model, Commerce, Find, edit/admin, and migration comprehension (CMS 11 → CMS 12).
- **[templates/](templates/)** — Version-pinned, genericized scaffolds (content types, controller+view, init module, scheduled job, Commerce, Find, packages.config, web.config sections).
- **[examples/](examples/)** — Annotated end-to-end slices (CMS page+rendering, Commerce cart/checkout, Find query).
- **[VALIDATION.md](VALIDATION.md)** — Coverage matrix and intentional scope boundaries.
- **`developing-with-dotnet-4x`** — the host .NET Framework 4.x platform layer (load alongside when the host targets `v4.x`).
