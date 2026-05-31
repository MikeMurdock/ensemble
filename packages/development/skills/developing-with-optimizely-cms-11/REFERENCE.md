# Optimizely / Episerver CMS 11 & Commerce 13 — Comprehensive Guide

In-depth companion to [SKILL.md](SKILL.md). This guide targets the **maintenance lifecycle** of an existing Optimizely/Episerver CMS 11 and Commerce 13 application: understanding the content and commerce models, changing them safely, diagnosing platform-specific failure modes, and comprehending the app well enough to migrate it. Validated against a representative deployed stack: **CMS 11.x, Commerce 13.x, Find 13.x on .NET Framework 4.6.1**.

> **Naming (read once):** Episerver → Optimizely is a **brand** change (c. 2020–2021). At CMS 11 / Commerce 13 the packages and namespaces are still `EPiServer.*` (Commerce internals `Mediachase.*`). The `Optimizely.*` package/namespace rename arrives at **CMS 12 / Commerce 14** on modern .NET. This guide uses "Optimizely" and "Episerver" interchangeably for the product and `EPiServer.*` for code.

## Table of Contents

1. [Relationship to the Platform Skill](#1-relationship-to-the-platform-skill)
2. [The Content Model](#2-the-content-model)
3. [Content Access & Events](#3-content-access--events)
4. [Initialization & Dependency Injection](#4-initialization--dependency-injection)
5. [Routing & MVC Rendering](#5-routing--mvc-rendering)
6. [Scheduled Jobs](#6-scheduled-jobs)
7. [Edit & Admin Integration](#7-edit--admin-integration)
8. [Commerce 13](#8-commerce-13)
9. [Search & Navigation (Find)](#9-search--navigation-find)
10. [Configuration](#10-configuration)
11. [Diagnostics & Troubleshooting](#11-diagnostics--troubleshooting)
12. [Migration Comprehension (CMS 11 → CMS 12)](#12-migration-comprehension-cms-11--cms-12)
13. [Version Reference](#13-version-reference)

---

## 1. Relationship to the Platform Skill

CMS 11 / Commerce 13 are **ASP.NET applications on .NET Framework 4.x**. This skill owns the **Optimizely layer**; the **`developing-with-dotnet-4x`** skill owns the **host platform** beneath it. The split:

| Concern | Skill |
|---------|-------|
| Old-style csproj, `packages.config`, NuGet restore, binding redirects | `developing-with-dotnet-4x` |
| `web.config` plumbing, OWIN/Katana startup, IIS, MSTest, sync-over-async | `developing-with-dotnet-4x` |
| ASP.NET MVC 5 mechanics (routing, controllers, Razor, filters) | `developing-with-dotnet-4x` (general) + this skill (Optimizely-specific controllers/views) |
| Content model, `IContentRepository`, init modules, scheduled jobs | **this skill** |
| Commerce catalog/cart/order/pricing, Find | **this skill** |

**Verify the host before assuming 4.x:** check `<TargetFrameworkVersion>` in the `.csproj` (or `targetFramework` in `packages.config`). If `v4.x`, load `developing-with-dotnet-4x` alongside this skill. This skill does not repeat the platform material.

---

## 2. The Content Model

### 2.1 Content types

All content is a CLR type decorated with an attribute and derived from a base. Optimizely synchronizes these types into the database on startup (keyed by `GUID`).

```csharp
[ContentType(
    DisplayName = "Article Page",
    GUID = "…unique…",
    Description = "An editorial article.",
    GroupName = "Acme")]
[AvailableContentTypes(
    Availability.Specific,
    IncludeOn = new[] { typeof(StandardPage) })]   // where it may be created
public class ArticlePage : SitePageData             // a site-specific base (see 2.2)
{
    [Display(Name = "Lead", Order = 10), CultureSpecific]
    public virtual string Lead { get; set; }

    [Display(Name = "Body", Order = 20), CultureSpecific]
    public virtual XhtmlString Body { get; set; }
}
```

| Base | Routable? | Purpose |
|------|-----------|---------|
| `PageData` | Yes (URL) | Pages |
| `BlockData` | No | Reusable composable blocks |
| `MediaData` / `ImageData` / `VideoData` | Asset URL | Media (often with a `[MediaDescriptor(ExtensionString=…)]`) |
| `ProductContent` / `VariationContent` / `NodeContent` / `BundleContent` | Catalog | Commerce — see §8 |

### 2.2 Base classes & `SitePageData`

Teams typically introduce an abstract `SitePageData : PageData` (and `SiteBlockData : BlockData`) to carry site-wide properties (SEO fields, visibility flags) so every page/block inherits them. This is a convention, not a framework requirement — look for it when orienting.

### 2.3 Property types & attributes

| Property type | Notes |
|---------------|-------|
| `string` | Single-line text; `[UIHint(UIHint.Textarea)]` for multi-line |
| `XhtmlString` | Rich text (TinyMCE) |
| `ContentReference` / `PageReference` | Pointer to other content |
| `ContentArea` | Ordered composition of blocks/content |
| `Url` | Internal/external link |
| `LinkItemCollection` | List of links |
| `Blob` | Binary payload (media) |
| `bool` / `int` / `double` / `DateTime` | Primitives |

Attributes: `[CultureSpecific]` (per-language), `[Required]`, `[Searchable]` (indexable), `[UIHint(...)]`, `[Display(GroupName, Order, Name, Description)]`, `[Editable]/[ScaffoldColumn]`, `[AllowedTypes]` (restrict what a `ContentArea`/reference accepts), `[CultureSpecific]`. **Properties must be `public virtual`** — Optimizely creates a runtime proxy that intercepts get/set to read/write the property bag. A non-virtual property silently won't persist.

### 2.4 Type sync gotchas

- The startup synchronizer maps each `[ContentType]` by `GUID`. Duplicate or changed GUIDs cause "missing type" / orphan behavior.
- Removing a property doesn't delete stored data immediately; the admin "content type" view shows mismatches.
- `[AvailableContentTypes]` governs which types editors can create where; missing it can make a type appear "uncreatable."

---

## 3. Content Access & Events

### 3.1 Loaders vs repository

```csharp
// READ — IContentLoader (cache-friendly, read-only results).
var page  = loader.Get<ArticlePage>(link);
var items = loader.GetChildren<PageData>(link);
var some  = loader.GetItems(links, culture);        // batch
loader.TryGet<ArticlePage>(link, out var maybe);

// WRITE — IContentRepository.
var writable = page.CreateWritableClone() as ArticlePage;
writable.Lead = "…";
repo.Save(writable, SaveAction.Publish, AccessLevel.NoAccess);

var created = repo.GetDefault<ArticlePage>(parent);
repo.Save(created, SaveAction.Publish);

repo.Move(link, newParent);
repo.Delete(link, forceDelete: true, AccessLevel.NoAccess);
```

> **Never mutate a loader result.** Instances are shared from cache; changing one is a race and corrupts other requests. `CreateWritableClone()` always.

### 3.2 `SaveAction`

`SaveAction.Save` (draft), `.Publish`, `.CheckIn`, `.CheckOut`, `.SkipValidation` (flag-combinable). The second `AccessLevel` argument controls the access check (`NoAccess` to bypass when running as system code).

### 3.3 Content events

`IContentEvents` exposes `PublishedContent`, `SavingContent`, `DeletingContent`, `MovedContent`, etc. Subscribe in an init module; **always unsubscribe in `Uninitialize`**. Use `SavingContent` to mutate before persistence (operate on `e.Content` which is already writable there).

### 3.4 References & URLs

Resolve a friendly URL via `IUrlResolver.GetUrl(contentLink)`. Resolve a `ContentReference` to content via the loader. `ContentReference.IsNullOrEmpty(link)` guards empty links. `PermanentLinkMapStore` / `UrlResolver` handle permanent (`~/link/…`) links embedded in `XhtmlString`.

---

## 4. Initialization & Dependency Injection

### 4.1 Module types

```csharp
[InitializableModule]
[ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
public class AppInitialization : IInitializableModule
{
    public void Initialize(InitializationEngine context) { /* wiring, event subscriptions */ }
    public void Uninitialize(InitializationEngine context) { /* tear down */ }
}

[InitializableModule]
public class ContainerInitialization : IConfigurableModule
{
    // Runs BEFORE the container is built — the place to register services.
    public void ConfigureContainer(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IProductService, ProductService>();
        context.StructureMap().Configure(c => c.For<ILegacyThing>().Use<LegacyThing>());
    }
    public void Initialize(InitializationEngine context) { }
    public void Uninitialize(InitializationEngine context) { }
}
```

- **Order** is by `[ModuleDependency(typeof(OtherModule))]`, not declaration order. Depend on `EPiServer.Web.InitializationModule` (the CMS initialization module) as needed; Commerce/Find add their own (e.g. a Find/Commerce initialization module).
- `context.Locate.Advanced.GetInstance<T>()` resolves services inside `Initialize`.
- `context.InitComplete += …` defers work until all modules initialized.

### 4.2 The StructureMap container

CMS 11's `IServiceLocator` is backed by **StructureMap**. `context.Services` is an abstraction (`IServiceConfigurationProvider`) with `AddTransient/AddSingleton/AddScoped`; drop to `context.StructureMap()` for advanced registrations. Resolution options, in order of preference:

1. **Constructor injection** (testable, explicit) — works for controllers, jobs, and services the container creates.
2. **`Injected<T>`** property — for types you can't constructor-inject cleanly.
3. **`ServiceLocator.Current.GetInstance<T>()`** — service-location; avoid except at composition edges (it hides dependencies and resists testing).

`[ServiceConfiguration(typeof(IFoo), Lifecycle = ServiceInstanceScope.Singleton)]` on an implementation auto-registers it.

### 4.3 Migration note

CMS 12 replaces StructureMap with **built-in ASP.NET Core DI** (`IServiceCollection` in `Startup.ConfigureServices`). `IConfigurableModule.ConfigureContainer` largely maps to that — see §12.

---

## 5. Routing & MVC Rendering

### 5.1 Content routing

Optimizely maps an incoming URL to a content item, then to a **template** (controller/view) for that content's type. You rarely write routes; you write typed controllers and the framework dispatches.

```csharp
public class ArticlePageController : PageController<ArticlePage>
{
    private readonly IContentLoader _loader;
    public ArticlePageController(IContentLoader loader) => _loader = loader;

    public ActionResult Index(ArticlePage currentPage)   // currentPage is model-bound from the route
    {
        var model = new ArticlePageViewModel(currentPage);
        return View(model);
    }
}

public class HeroBlockController : BlockController<HeroBlock>
{
    public override ActionResult Index(HeroBlock currentBlock) => PartialView(currentBlock);
}
```

- `PageController<T>` / `BlockController<T>` / `PartialContentController<T>` / `ContentController<T>` — base classes that bind `currentPage` / `currentBlock`.
- Template selection can be influenced by `[TemplateDescriptor(...)]` (tags, default, inherited).

### 5.2 Razor rendering & on-page editing

```cshtml
@model Acme.Web.Models.ViewModels.ArticlePageViewModel
@{ Layout = "~/Views/Shared/_Layout.cshtml"; }

<article>
    <h1>@Html.PropertyFor(m => m.CurrentPage.Heading)</h1>
    @Html.PropertyFor(m => m.CurrentPage.Body)
    @Html.PropertyFor(m => m.CurrentPage.MainContentArea)
</article>
```

- **Always** render editable properties with `Html.PropertyFor` — it emits the data attributes the on-page editor needs and applies the correct display template per property type.
- `Html.PropertyFor(m => m.X, new { Tag = "…" })` selects a rendering tag (e.g. a wide vs narrow block rendering).
- A `ContentArea` renders its items through their block controllers/templates automatically.
- Display templates live in `~/Views/Shared/DisplayTemplates/` keyed by type (e.g. `XhtmlString.cshtml`).

### 5.3 View models

Convention: wrap `currentPage` in a view model implementing `IPageViewModel<T>` (often via a layout/`IContentViewModel` pattern) so layout data (menus, site settings) travels with the page model. Look for a `PageViewModel<T>` / `IPageViewModel<T>` base when orienting.

---

## 6. Scheduled Jobs

```csharp
[ScheduledPlugIn(
    DisplayName = "Acme Catalog Import",
    GUID = "…",
    IntervalType = ScheduledIntervalType.Hours,
    IntervalLength = 6,
    Restartable = true)]
public class CatalogImportJob : ScheduledJobBase
{
    private bool _stopSignaled;
    public CatalogImportJob() { IsStoppable = true; }

    public override void Stop() => _stopSignaled = true;

    public override string Execute()
    {
        int n = 0;
        foreach (var item in GetWork())
        {
            if (_stopSignaled) return $"Stopped after {n}.";
            Process(item);
            n++;
            if (n % 100 == 0) OnStatusChanged($"Processed {n}…");
        }
        return $"Completed: {n} items.";
    }
}
```

- Derive from `ScheduledJobBase`; the `[ScheduledPlugIn]` attribute registers it (admin UI shows it).
- `IsStoppable = true` + honoring `Stop()` lets editors cancel a run.
- `OnStatusChanged(text)` surfaces progress; the returned string is the run's result message.
- Jobs run on a background thread without an HTTP context — resolve dependencies via the container, not `HttpContext`.

---

## 7. Edit & Admin Integration

- **Edit mode** (`/EPiServer/CMS`): on-page + forms editing. `Html.PropertyFor` is what makes properties editable in context.
- **Admin mode**: content-type sync status, scheduled jobs, access rights, content approval.
- **Properties UI**: `[Display]`, `[UIHint]`, `[Editable]`, `[ScaffoldColumn(false)]`, custom `EditorDescriptor`s and `SelectionFactory` (dropdowns) shape the editing UX.
- **Access rights**: content has ACLs; `FilterForVisitor`/access checks honor them. Virtual roles (`EPiServer.Security`) map app concepts to CMS roles.
- **Dojo/Shell**: the edit UI is a Dojo app; custom editors integrate via client resources and `module.config`. Treat deep Dojo customization as specialized.

---

## 8. Commerce 13

### 8.1 Catalog content model

Commerce 13 models the catalog as **content** (same infrastructure as CMS pages), under a catalog root:

```
Catalog
└── NodeContent (category)
    └── ProductContent (product — groups variations, usually not directly buyable)
        └── VariationContent (SKU — the buyable unit; has price + inventory)
    └── BundleContent / PackageContent (compositions)
```

```csharp
[CatalogContentType(GUID = "…", DisplayName = "Product", MetaClassName = "AcmeProduct")]
public class AcmeProduct : ProductContent
{
    [Display(Name = "Description", Order = 10), CultureSpecific]
    public virtual XhtmlString Description { get; set; }
}

[CatalogContentType(GUID = "…", DisplayName = "Variation", MetaClassName = "AcmeVariation")]
public class AcmeVariation : VariationContent
{
    [Display(Name = "Color", Order = 10)]
    public virtual string Color { get; set; }
}
```

Load catalog content with the same `IContentLoader`/`IContentRepository` (catalog content links are `ContentReference`s). `ReferenceConverter` converts between catalog codes and content links.

### 8.2 The order system

The cart/order abstractions (`EPiServer.Commerce.Order`) sit over the legacy `Mediachase.Commerce.Orders` foundation:

| Abstraction | Role |
|-------------|------|
| `IOrderRepository` | Load/save carts and purchase orders |
| `ICart` | A shopping cart (a kind of `IOrderGroup`) |
| `IPurchaseOrder` | A completed order |
| `IOrderForm` / `IShipment` / `ILineItem` | Order structure |
| `IOrderGroupFactory` | Creates line items, shipments, etc. |

```csharp
var cart = _orderRepository.LoadOrCreateCart<ICart>(contactId, "Default");
var shipment = cart.GetFirstShipment();
var line = cart.CreateLineItem(variationCode);
line.Quantity = 2;
shipment.LineItems.Add(line);
_orderRepository.Save(cart);                       // persist cart

var orderRef = _orderRepository.SaveAsPurchaseOrder(cart);   // checkout
var order = _orderRepository.Load<IPurchaseOrder>(orderRef.OrderGroupId);
```

Validation/recalculation: `IOrderGroupCalculator` / `IShippingCalculator` / `ILineItemCalculator` compute totals; call validation before checkout (`IOrderGroup.ValidateOrRemoveLineItems`).

### 8.3 Pricing & promotions

```csharp
IPriceService priceService;                 // raw price rows
IPriceDetailService priceDetailService;     // CRUD on price details (List/Save/Delete by content link)
var prices = priceDetailService.List(variation.ContentLink);

IPromotionEngine promotionEngine;           // applies campaigns/discounts to an order group
var rewards = promotionEngine.Run(cart);
```

`IMarketService` defines markets (currency, language, price/inventory scoping). Prices and inventory are market-aware — a missing/incorrect market is a common cause of "no price" bugs.

### 8.4 Inventory

`IInventoryService` (and `IWarehouseRepository`) track stock per variation per warehouse. Catalog content + inventory + price together make a variation purchasable.

### 8.5 The `Mediachase.*` layer

The original Commerce foundation (`Mediachase.Commerce.Catalog`, `.Orders`, `.Customers`, `.Pricing`, `.Marketing`, plus `Mediachase.BusinessFoundation` — the "BF" metadata system) underlies the modern abstractions. Legacy code often calls it directly (e.g. `CustomerContext.Current.CurrentContact`). Prefer the `EPiServer.Commerce.*` abstractions for new work; understand `Mediachase.*` to read existing code.

---

## 9. Search & Navigation (Find)

### 9.1 The client

`EPiServer.Find` indexes content (and POCOs) into a hosted index and queries it with a typed, fluent API. Get the client via `IClient` (injected) or `SearchClient.Instance`.

```csharp
var result = client.Search<ArticlePage>()
    .For("annual report")                       // free-text across analyzed fields
    .InField(x => x.Heading)                     // optionally scope the text query
    .Filter(x => x.Category.Match("news"))       // typed structured filter
    .FilterForVisitor()                          // access + publish-status (front-end)
    .OrderByDescending(x => x.StartPublish)
    .Skip(0).Take(10)
    .GetContentResult();                         // returns IContent items (CMS integration)
```

### 9.2 Query building

- `.Search<T>()` starts a typed query; `T` can be a content type or any indexed POCO.
- `.For(text)` is free-text; `.InField(...)` / `.UsingSynonyms()` refine it.
- `.Filter(expr)` adds structured filters; `FilterBuilder<T>` composes boolean logic (`&`, `|`, `!`). String fields expose `.Match`, `.Prefix`, `.AnyWordBeginsWith`; collections `.In`; ranges `.InRange`.
- `.GetResult()` returns the typed hits (`SearchResults<T>`); `.GetContentResult()` hydrates CMS `IContent`.
- Pagination via `.Skip().Take()`; facets via `.TermsFacetFor(...)`; projection via `.Select(...)`.

### 9.3 Indexing

- Content is indexed automatically on publish via Find's CMS integration (an initialization module wires the event conventions).
- `[Searchable]` and indexing conventions (`client.Conventions`) control what's indexed and how.
- Re-index via the **Find indexing scheduled job** (admin) after convention changes or bulk imports.
- `client.Index(obj)` / `.Delete(...)` for manual/POCO indexing.

### 9.4 Find vs the legacy search

`EPiServer.Search` (Lucene-based built-in search, often paired with `EPiServer.Search.Cms`) predates Find and may coexist in a CMS 11 site. It uses a different API (`SearchHandler`, `IndexingController`). This skill features **Find**; recognize `EPiServer.Search` as the older option and don't conflate their APIs.

---

## 10. Configuration

CMS 11 configuration lives in `web.config` (the platform file owned by `developing-with-dotnet-4x`), with **Optimizely-specific sections**:

```xml
<configSections>
  <section name="episerver" type="EPiServer.Configuration.EPiServerSection, EPiServer.Configuration" />
  <section name="episerver.framework" type="EPiServer.Framework.Configuration.EPiServerFrameworkSection, EPiServer.Framework" />
  <section name="episerver.commerce" type="…Mediachase…/EPiServer.Commerce…" />   <!-- if Commerce -->
  <section name="episerver.find" type="EPiServer.Find.Configuration, EPiServer.Find" />  <!-- if Find -->
</configSections>

<episerver.framework>
  <appData basePath="App_Data" />
  <scanAssembly forceBinFolderScan="true" />     <!-- module/type discovery -->
  <virtualRoles>…</virtualRoles>
</episerver.framework>

<episerver.find serviceUrl="https://…/" defaultIndex="acme-index" />

<connectionStrings>
  <add name="EPiServerDB" connectionString="…" providerName="System.Data.SqlClient" />
  <add name="EcfSqlConnection" connectionString="…" providerName="System.Data.SqlClient" />  <!-- Commerce -->
</connectionStrings>
```

- `EPiServerDB` is the CMS database; `EcfSqlConnection` is the Commerce database. Both are SQL Server.
- Find needs `serviceUrl` + `defaultIndex` (the index name). The service URL embeds an index key — treat it as a secret (config transform / encrypted section; see `developing-with-dotnet-4x` on web.config transforms & section protection).
- Module discovery scans `bin`; `protectedModules` / `modules` register the Shell/CMS UI packages.

---

## 11. Diagnostics & Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Content edits lost / intermittent | Mutated a cache-shared `IContent` | `CreateWritableClone()`, then `Save` |
| Property value never persists | Not `public virtual` (no proxy) | Make property `public virtual` |
| New content type missing in edit UI | GUID collision / sync failure / no `[AvailableContentTypes]` | Unique GUID; check startup sync; set availability |
| On-page editing dead for a field | Rendered via `@Model.X` | `Html.PropertyFor(m => m.X)` |
| Init module runs too early/late | Missing `[ModuleDependency]` | Order via dependencies; use `InitComplete` |
| `ServiceLocator` fails during init | Resolved before container built | Register in `ConfigureContainer`; resolve in `Initialize` |
| Find results stale/empty | Index outdated / no visitor filter | Run Find indexing job; add `.FilterForVisitor()` |
| Commerce "no price"/"no inventory" | Wrong market/currency or unsaved cart | Resolve market via `IMarketService`; `Save` after mutation |
| Catalog content not found by code | Used catalog code vs content link | Convert with `ReferenceConverter` |
| Boot-time assembly/version errors | Binding redirects (platform) | See `developing-with-dotnet-4x` §8 |
| Editor UI (Dojo) broken after deploy | `protectedModules`/Shell version mismatch | Verify UI package versions + module config |

> Host-platform failures (binding redirects, sync-over-async deadlocks, web.config transforms, IIS) are diagnosed in **`developing-with-dotnet-4x`**. Everything above is the Optimizely layer.

### Useful diagnostics

- The site logs (Optimizely uses the configured logger; older sites use log4net via `EPiServerLog.config`) capture init and content errors.
- Admin → Scheduled Jobs shows job history and last status messages.
- Admin → Content Type analysis flags type/property mismatches.
- For Find, the developer/explore view and the query result's `.ProcessedQuery` help debug queries.

---

## 12. Migration Comprehension (CMS 11 → CMS 12)

> A full CMS 11 → CMS 12 upgrade (the package/namespace rename, .NET Framework → modern .NET, StructureMap → built-in DI, `web.config` → `appsettings.json`/`Program.cs`) is a **distinct effort** and not performed by this skill. This section is *comprehension and mapping* — enough to read a CMS 11 app and understand what an upgrade entails.

### 12.1 What changes

| CMS 11 / Commerce 13 (.NET Framework 4.x) | CMS 12 / Commerce 14 (modern .NET) |
|-------------------------------------------|------------------------------------|
| Packages/namespaces `EPiServer.*` | Renamed to `Optimizely.*` (CMS APIs largely the same shape) |
| .NET Framework 4.x host | .NET 5+ (`Program.cs`, Kestrel) |
| StructureMap via `IConfigurableModule.ConfigureContainer` | Built-in DI in `Startup.ConfigureServices` |
| `web.config` (XML) + Optimizely sections | `appsettings.json` + options/`AddCms()` |
| MVC 5 (`System.Web.Mvc`) controllers/views | ASP.NET Core MVC (`Microsoft.AspNetCore.Mvc`) |
| `Global.asax` / `InitializationModule` | Host builder + initialization modules (still supported) |
| `Mediachase.*` Commerce foundation | Trimmed / partly replaced in Commerce 14 |
| Binding redirects | None (SDK-style deps) |

### 12.2 What carries over (mostly)

The **content model is the most portable part**: `[ContentType]`, `PageData`/`BlockData`, property types, `IContentRepository`/`IContentLoader`, initialization modules, and scheduled jobs exist in CMS 12 with similar shapes (renamed namespaces). The bulk of the effort is the **host migration** (the platform skill's `developing-with-dotnet-4x` → modern-.NET concern), the **DI/config re-platform**, and **Commerce 14** API deltas.

### 12.3 Reading a CMS 11 app for upgrade

1. **Inventory versions** — confirm `EPiServer.*` 11.x/13.x and the host `TargetFrameworkVersion` (see §1).
2. **Catalog content types** — they migrate with namespace changes; note custom `EditorDescriptor`s and `PropertyFor` templates.
3. **Map initialization & DI** — every `IConfigurableModule.ConfigureContainer` becomes service registration on the modern host.
4. **Find `System.Web` / `web.config` coupling** — this is the host concern (`developing-with-dotnet-4x` migration section).
5. **Commerce** — assess `Mediachase.*` direct usage; those are the riskiest to carry to Commerce 14.
6. **Find** — query/convention code largely ports; re-validate indexing conventions.

---

## 13. Version Reference

### 13.1 Validated baseline

| Component | Package(s) | Version (baseline) |
|-----------|-----------|--------------------|
| Host runtime | .NET Framework | 4.6.1 (see `developing-with-dotnet-4x`) |
| CMS core | `EPiServer.CMS` / `CMS.Core` / `Framework` | 11.x |
| CMS UI | `EPiServer.CMS.UI` / `UI.Core` | 11.x |
| TinyMCE | `EPiServer.CMS.TinyMce` | 2.x |
| Commerce | `EPiServer.Commerce` / `Commerce.Core` / `Commerce.UI` | 13.x |
| Search & Navigation | `EPiServer.Find` / `Find.Cms` / `Find.Framework` | 13.x |
| Find for Commerce | `EPiServer.Find.Commerce` | 11.x (Commerce-aligned) |
| DI | `EPiServer.ServiceLocation.StructureMap` over `StructureMap` | 2.x / 4.x |
| Legacy search (if present) | `EPiServer.Search` / `Search.Cms` | 9.x |

> Pins reflect a representative deployed CMS 11 baseline. Within CMS 11 / Commerce 13, point releases are API-compatible; treat the exact restored versions in the project's `packages.config` as the source of truth. CMS 11 / Commerce 13 are mature/long-term-support era products — these are maintenance baselines, not an endorsement of remaining on them indefinitely.

### 13.2 The naming/version boundary

| You see… | It means… |
|----------|-----------|
| `EPiServer.*` packages, 11.x / 13.x, `v4.x` host | CMS 11 / Commerce 13 — **this skill** |
| `Optimizely.*` packages, `net5.0`+ host | CMS 12+ / Commerce 14+ — out of scope (migration target) |
| Docs say "Optimizely" but code says `EPiServer` | Normal at this version — branding vs packages |

### 13.3 See Also

- [SKILL.md](SKILL.md) — quick reference and decision tables
- [templates/](templates/) — version-pinned scaffolds
- [examples/](examples/) — annotated end-to-end slices
- [VALIDATION.md](VALIDATION.md) — coverage matrix and scope boundaries
- **`developing-with-dotnet-4x`** — the host .NET Framework 4.x platform layer
