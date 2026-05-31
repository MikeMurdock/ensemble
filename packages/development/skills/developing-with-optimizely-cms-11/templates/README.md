# Optimizely / Episerver CMS 11 & Commerce 13 Templates

Version-pinned, genericized scaffolds for maintaining Optimizely/Episerver CMS 11, Commerce 13, and Search & Navigation (Find) applications. All templates use a neutral `Acme.*` namespace and generic content/catalog names — replace the placeholders below.

> **Naming reminder:** at CMS 11 / Commerce 13 the packages and namespaces are `EPiServer.*` (the Optimizely rename lands at CMS 12+). Templates use `EPiServer.*` accordingly.

> **Platform layer:** these scaffold the Optimizely layer only. The host project (`.csproj`, `web.config` plumbing, `packages.config` base, binding redirects, OWIN, MSTest) is covered by the **`developing-with-dotnet-4x`** skill — load it alongside when the host targets `v4.x`.

## Available Templates

### 1. ContentType.template.cs
**Page + block content types** (`PageData` / `BlockData`) with `[ContentType]`, `public virtual` properties, and common attributes (`[Display]`, `[CultureSpecific]`, `[Required]`, `[UIHint]`).
**Placeholders**: `{{Namespace}}`, `{{Page}}`, `{{Block}}`

### 2. PageController.template.cs
**`PageController<T>` + view model** — model-binds the routed page and loads children via `IContentLoader`.
**Placeholders**: `{{Namespace}}`, `{{Page}}`

### 3. PageView.template.cshtml
**Razor 3 view** rendering properties through `Html.PropertyFor` (so on-page editing works), including a `ContentArea`.
**Placeholders**: `{{Namespace}}`, `{{Page}}`

### 4. InitializationModule.template.cs
**`IConfigurableModule` (DI registration) + `IInitializableModule` (content-event wiring)** with `[ModuleDependency]` ordering and proper `Uninitialize` unsubscription.
**Placeholders**: `{{Namespace}}`

### 5. ScheduledJob.template.cs
**`ScheduledJobBase` job** with `[ScheduledPlugIn]`, stoppable execution, and progress reporting.
**Placeholders**: `{{Namespace}}`, `{{Job}}`

### 6. CommerceCart.template.cs
**Commerce 13 cart + pricing service** — `IOrderRepository` load/save, line items/shipments, checkout to `IPurchaseOrder`, `IPriceDetailService`.
**Placeholders**: `{{Namespace}}`

### 7. FindQuery.template.cs
**Search & Navigation (Find) query service** — `IClient.Search<T>()` with typed filters, `FilterForVisitor`, pagination, and a `BuildFilter` example.
**Placeholders**: `{{Namespace}}`, `{{Page}}`

### 8. packages.config.template
**The `EPiServer.*` dependency closure** (CMS 11 / Commerce 13 / Find 13 + StructureMap DI) layered on top of the base ASP.NET stack.

### 9. web.config.episerver.template
**The Optimizely-specific `web.config` sections** — `configSections`, `EPiServerDB`/`EcfSqlConnection`, `episerver.framework`, and `episerver.find`.
**Placeholders**: `{{IndexName}}`

## Placeholder Conventions

| Placeholder | Meaning | Example |
|-------------|---------|---------|
| `{{Namespace}}` | Root namespace | `Acme.Web` |
| `{{Page}}` | Page content type (PascalCase) | `StandardPage` |
| `{{Block}}` | Block content type (PascalCase) | `HeroBlock` |
| `{{Job}}` | Scheduled job base name | `NightlySync` |
| `{{IndexName}}` | Find index name | `acme-index` |

## Usage

```bash
# Copy and substitute (macOS/BSD sed shown; use sed -i on GNU)
cp templates/ContentType.template.cs Models/StandardPage.cs
sed -i '' 's/{{Namespace}}/Acme.Web/g; s/{{Page}}/StandardPage/g; s/{{Block}}/HeroBlock/g' Models/StandardPage.cs
```

> **Replace the placeholder GUIDs** (`00000000-…`) in content types and scheduled jobs with unique GUIDs — duplicates cause type-sync conflicts.

## Related Documentation

- [SKILL.md](../SKILL.md) — quick reference
- [REFERENCE.md](../REFERENCE.md) — comprehensive guide
- [examples/](../examples/) — annotated end-to-end slices
- **`developing-with-dotnet-4x`** — the host platform templates (csproj, base web.config, packages.config, OWIN, MSTest)
