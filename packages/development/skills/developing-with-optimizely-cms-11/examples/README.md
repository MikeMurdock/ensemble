# Optimizely / Episerver CMS 11 & Commerce 13 Examples

Annotated, end-to-end slices for maintaining Optimizely/Episerver CMS 11, Commerce 13, and Search & Navigation (Find). Genericized (`Acme.*` / `StandardPage` / generic catalog) and self-documenting — read them to see how the layers connect, then adapt to your codebase.

> **Naming:** "Optimizely CMS 11" == "Episerver CMS 11"; code/namespaces are `EPiServer.*` at this version (the `Optimizely.*` rename is CMS 12+).

> **Platform layer:** these show the Optimizely layer only. The host (`.csproj`, `web.config` plumbing, binding redirects, OWIN, MSTest) is covered by **`developing-with-dotnet-4x`** — pair the two when the host targets `v4.x`.

## Available Examples

### 1. CmsContentSlice.example.cs
**A full CMS content slice.** Content type (`PageData`) → `PageController<T>` → view model → read/publish service (`IContentLoader` / `IContentRepository`), plus a publish-event handler registered in an `IConfigurableModule`. Demonstrates the cardinal rule: `CreateWritableClone()` before mutating cache-shared content.

**Use when**: adding or refactoring a content type and its rendering/handling in a CMS 11 site.

### 2. CommerceCheckout.example.cs
**A Commerce 13 cart → checkout slice.** Catalog `VariationContent` → cart line items via `IOrderRepository`/`ICart` → pricing via `IPriceDetailService` → `SaveAsPurchaseOrder` → `IPurchaseOrder`. Notes market/currency scoping and the validate-before-checkout step.

**Use when**: working on cart, pricing, or checkout logic in a Commerce 13 site.

### 3. FindSearch.example.cs
**A Search & Navigation (Find) query slice.** Typed `IClient.Search<T>()` with free-text + `InField` + `FilterForVisitor` + paging → results view model → controller. Shows the front-end safety filter and CMS content hydration.

**Use when**: building or debugging Find-backed search on a CMS 11 site.

## How to Use These Examples

These are **illustrative single-file slices** — in a real solution the types live across `Models/`, `Controllers/`, `Business/Initialization/`, and a Commerce project. They are written to be read top-to-bottom; the comments carry the lesson.

```bash
# Read a slice
sed -n '1,60p' examples/CmsContentSlice.example.cs
```

> Not wired into a buildable solution here (no csproj/restore). To compile, drop the types into a project that references the packages in [../templates/packages.config.template](../templates/packages.config.template) plus the base ASP.NET stack from `developing-with-dotnet-4x`, and restore.

## Related Documentation

- [SKILL.md](../SKILL.md) — quick reference
- [REFERENCE.md](../REFERENCE.md) — comprehensive guide
- [templates/](../templates/) — version-pinned scaffolds
- **`developing-with-dotnet-4x`** — the host platform layer
