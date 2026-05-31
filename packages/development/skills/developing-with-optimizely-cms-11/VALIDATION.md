# Optimizely / Episerver CMS 11 & Commerce 13 Skill — Validation Report

**Status**: Maintenance-focused | **Validated baseline**: Optimizely/Episerver CMS 11.x, Commerce 13.x, Find 13.x on .NET Framework 4.6.1
**Framing**: legacy maintenance — debug / modify / migrate existing sites (not greenfield)
**Composition**: specialization layered on `developing-with-dotnet-4x` (host platform); load both when host is `v4.x`

---

## Naming Coverage

| Concern | Covered | Location |
|---------|---------|----------|
| Episerver ↔ Optimizely rebrand (branding only at v11) | Yes | SKILL naming callout, REFERENCE intro |
| Packages/namespaces are `EPiServer.*` at CMS 11/Commerce 13 | Yes | SKILL, REFERENCE §13.2, all templates |
| `Optimizely.*` rename = CMS 12+/Commerce 14+ boundary | Yes | SKILL scope, REFERENCE §12/§13.2 |
| Mediachase.* legacy Commerce foundation | Yes | SKILL Commerce, REFERENCE §8.5 |

---

## Feature Parity Matrix

### CMS Content Model

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Content types (`[ContentType]`, PageData/BlockData) | Yes | SKILL §4, REFERENCE §2, templates/ContentType | public virtual rule |
| Media types (MediaData/ImageData/VideoData) | Yes | SKILL §4, REFERENCE §2.1 | MediaDescriptor noted |
| Property types & attributes | Yes | REFERENCE §2.3 | XhtmlString, ContentArea, UIHint, CultureSpecific |
| Base classes / SitePageData convention | Yes | REFERENCE §2.2 | Site-wide property pattern |
| Content type sync & GUID gotchas | Yes | REFERENCE §2.4, SKILL debugging | Duplicate GUID failure |

### Content Access

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| IContentLoader (reads) | Yes | SKILL §5, REFERENCE §3.1 | Get/GetChildren/TryGet |
| IContentRepository (writes) | Yes | SKILL §5, REFERENCE §3.1 | CreateWritableClone, Save, GetDefault |
| SaveAction / AccessLevel | Yes | REFERENCE §3.2 | Publish/Save/SkipValidation |
| Content events (IContentEvents) | Yes | REFERENCE §3.3, examples/CmsContentSlice | subscribe/unsubscribe |
| URL/reference resolution | Yes | REFERENCE §3.4 | IUrlResolver, ContentReference |

### Initialization & DI

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| IInitializableModule | Yes | SKILL §6, REFERENCE §4.1, templates/InitializationModule | Initialize/Uninitialize |
| IConfigurableModule / ConfigureContainer | Yes | SKILL §7, REFERENCE §4.1 | DI registration |
| ModuleDependency ordering | Yes | SKILL §6, REFERENCE §4.1 | not declaration order |
| StructureMap container | Yes | SKILL §7, REFERENCE §4.2 | Injected<T>, ServiceLocator caveats |

### Rendering

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| PageController<T> / BlockController<T> | Yes | SKILL §8, REFERENCE §5.1, templates/PageController | model binding |
| Razor + Html.PropertyFor | Yes | SKILL §8, REFERENCE §5.2, templates/PageView | on-page editing |
| ContentArea rendering | Yes | REFERENCE §5.2 | block dispatch |
| View models (IPageViewModel) | Yes | REFERENCE §5.3 | layout data |
| TemplateDescriptor | Noted | REFERENCE §5.1 | tags/default |

### Scheduled Jobs

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| ScheduledJobBase + [ScheduledPlugIn] | Yes | SKILL §9, REFERENCE §6, templates/ScheduledJob | stoppable, status |

### Edit/Admin

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Edit/admin mode overview | Yes | REFERENCE §7 | on-page + forms |
| Properties UI (UIHint/EditorDescriptor) | Yes | REFERENCE §7 | SelectionFactory noted |
| Access rights / virtual roles | Yes | REFERENCE §7 | FilterForVisitor |
| Dojo/Shell customization | Noted | REFERENCE §7 | flagged specialized |

### Commerce 13

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Catalog content model | Yes | SKILL §10, REFERENCE §8.1, templates/CommerceCart | Node/Product/Variation |
| Order system (cart/order) | Yes | SKILL §10, REFERENCE §8.2, examples/CommerceCheckout | IOrderRepository/ICart/ILineItem |
| Pricing | Yes | SKILL §10, REFERENCE §8.3 | IPriceService/IPriceDetailService |
| Promotions | Yes | REFERENCE §8.3 | IPromotionEngine |
| Markets | Yes | REFERENCE §8.3 | IMarketService |
| Inventory | Yes | REFERENCE §8.4 | IInventoryService |
| Mediachase.* foundation | Yes | REFERENCE §8.5 | legacy layer |

### Search & Navigation (Find)

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Client (IClient/SearchClient) | Yes | SKILL §11, REFERENCE §9.1, templates/FindQuery | injection |
| Query building (For/Filter/FilterBuilder) | Yes | SKILL §11, REFERENCE §9.2, examples/FindSearch | typed filters |
| FilterForVisitor | Yes | SKILL §11, REFERENCE §9.2 | front-end safety |
| Indexing & conventions | Yes | REFERENCE §9.3 | auto-index on publish, re-index job |
| Find vs legacy EPiServer.Search | Yes | SKILL §11, REFERENCE §9.4 | distinct APIs |

### Configuration

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| episerver config sections | Yes | REFERENCE §10, templates/web.config.episerver | configSections |
| Connection strings (EPiServerDB/EcfSqlConnection) | Yes | REFERENCE §10 | CMS + Commerce DBs |
| Find serviceUrl/index (secret) | Yes | REFERENCE §10, templates/web.config.episerver | transform/encrypt |
| Module scanning | Yes | REFERENCE §10 | scanAssembly |

### Migration Comprehension

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| CMS 11 → CMS 12 mapping | Yes | REFERENCE §12.1 | package/DI/host changes |
| What carries over | Yes | REFERENCE §12.2 | content model portability |
| Reading an app for upgrade | Yes | REFERENCE §12.3 | inventory steps |

---

## Template Coverage

| Template | Purpose | Placeholders | Status |
|----------|---------|--------------|--------|
| ContentType.template.cs | Page + block content types | Namespace, Page, Block | Complete |
| PageController.template.cs | PageController<T> + view model | Namespace, Page | Complete |
| PageView.template.cshtml | Razor view with PropertyFor | Namespace, Page | Complete |
| InitializationModule.template.cs | Init + DI modules | Namespace | Complete |
| ScheduledJob.template.cs | Scheduled job | Namespace, Job | Complete |
| CommerceCart.template.cs | Commerce cart + pricing | Namespace | Complete |
| FindQuery.template.cs | Find query service | Namespace, Page | Complete |
| packages.config.template | EPiServer.* closure | — | Complete |
| web.config.episerver.template | Optimizely config sections | IndexName | Complete |

## Example Coverage

| Example | Patterns Demonstrated | Status |
|---------|----------------------|--------|
| CmsContentSlice.example.cs | content type ↔ controller ↔ service ↔ repository + events | Complete |
| CommerceCheckout.example.cs | variation → cart → pricing → purchase order | Complete |
| FindSearch.example.cs | typed Find query → results view model → controller | Complete |

---

## Validation Checklist

### Documentation Quality
- [x] SKILL.md provides a quick reference with decision tables
- [x] REFERENCE.md provides a comprehensive guide + troubleshooting matrix
- [x] All code examples are syntactically valid C# / Razor for the EPiServer 11 API
- [x] Versions framed against a validated CMS 11 / Commerce 13 / Find 13 baseline
- [x] Legacy-maintenance framing consistent (not greenfield)
- [x] Episerver/Optimizely naming clarified up front and used consistently

### Composition (one-way dependency)
- [x] References `developing-with-dotnet-4x` for the platform layer, conditional on detecting `v4.x`
- [x] Does NOT duplicate platform material (csproj/web.config plumbing/EF6/OWIN/binding redirects)
- [x] Platform skill has no reverse dependency on this skill

### Confidentiality (public-grade)
- [x] No client identifiers, repo names, domains, or internal component names
- [x] Content/catalog examples use neutral `Acme.*` / `StandardPage` / generic catalog vocabulary
- [x] Derived from canonical Optimizely/Episerver framework patterns, not copied source
- [x] Leak-scanned for client identifiers (content-type/domain nouns, org/product/repo names) before commit

---

## Scope Boundaries (Intentional Exclusions)

| Topic | Why excluded | Where it belongs |
|-------|--------------|------------------|
| Host .NET Framework platform (csproj, web.config plumbing, EF6, OWIN, binding redirects, MSTest) | Separate layer | `developing-with-dotnet-4x` |
| Optimizely CMS 12+ / Commerce 14+ (`Optimizely.*`, modern .NET) | Different packages/host/DI | Out of scope; migration target (a future upgrade skill) |
| Identity/authentication mechanisms | Auth-agnostic by design | Bring your own; OWIN integration via `developing-with-dotnet-4x` |
| Deep Dojo/Shell edit-UI customization | Specialized client-side surface | Optimizely UI docs |
| Add-ons not in the validated baseline (e.g. Forms, Profile Store, Personalization, DXP/CMP cloud) | Out of baseline scope | Add-on-specific docs |

---

## Recommendations

### For Skill Users
1. **Start with SKILL.md** for orientation; confirm the host framework and load `developing-with-dotnet-4x` if `v4.x`.
2. **Consult REFERENCE.md** for the full content/commerce/Find surface and migration comprehension.
3. **Copy templates**; replace placeholder GUIDs with unique values.
4. **Read examples** to see how the tiers connect before refactoring.

### For Skill Maintainers
1. Keep version pins aligned to the project's restored `packages.config` (CMS 11 / Commerce 13 / Find 13).
2. Keep the Episerver↔Optimizely naming clarification current as docs drift toward "Optimizely".
3. Re-run a leak scan for client identifiers (content-type/domain nouns, org/product/repo names) before any commit — examples must stay on the neutral `Acme.*` domain.
4. Expand the CMS 11 → CMS 12 migration mapping (REFERENCE §12) as a dedicated upgrade skill is developed.
