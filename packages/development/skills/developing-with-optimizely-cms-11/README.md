# Optimizely / Episerver CMS 11 & Commerce 13 Skill

Maintenance patterns for the **Optimizely/Episerver layer** of long-lived CMS 11 and Commerce 13 applications, including **Search & Navigation (Find)**. This skill helps you **understand, debug, modify, and migrate** existing Optimizely/Episerver sites.

> **Naming:** Episerver was rebranded **Optimizely** (c. 2020–2021). At CMS 11 / Commerce 13 the rename is **branding only** — packages and namespaces are still `EPiServer.*` (Commerce internals `Mediachase.*`). Newer docs say "Optimizely"; older docs and the code say "Episerver". The `Optimizely.*` package rename arrives at CMS 12 / Commerce 14 (modern .NET, out of scope here).

> **Not a greenfield skill.** New sites should target current Optimizely on modern .NET. This skill covers the *legacy* CMS 11 / Commerce 13 line and is the **comprehension/source side** for an eventual upgrade.

## A layered skill

CMS 11 / Commerce 13 are **ASP.NET applications on .NET Framework 4.x**. This skill owns the **Optimizely layer**; the host platform is owned by the **`developing-with-dotnet-4x`** skill. They compose:

- **This skill** — content types, `IContentRepository`/`IContentLoader`, initialization modules, scheduled jobs, MVC/Razor rendering of content, the Commerce catalog/cart/order/pricing model, and Find.
- **`developing-with-dotnet-4x`** — the old-style project system, `web.config` plumbing and binding redirects, EF6, OWIN/Katana, MSTest, and IIS-era debugging.

> **Verify, don't assume.** CMS 11 / Commerce 13 almost always run on .NET Framework 4.x, but confirm via `<TargetFrameworkVersion>` in the `.csproj` (or `targetFramework` in `packages.config`). If it's `v4.x`, load `developing-with-dotnet-4x` alongside this skill. This skill does not repeat the platform material (one-way dependency: this skill references the platform skill, never the reverse).

## Overview

Comprehensive guidance focused on:

- **Content model** — `[ContentType]`, `PageData`/`BlockData`/`MediaData`, property types and attributes, the `public virtual` proxy rule, type-sync gotchas
- **Content access** — `IContentLoader` (reads) vs `IContentRepository` (writes), `CreateWritableClone`, content events
- **Initialization & DI** — `IInitializableModule` / `IConfigurableModule`, `[ModuleDependency]` ordering, the StructureMap container
- **Rendering** — `PageController<T>` / `BlockController<T>`, `Html.PropertyFor`, content areas, view models
- **Scheduled jobs** — `ScheduledJobBase` + `[ScheduledPlugIn]`
- **Commerce 13** — catalog content (`Product`/`Variation`/`Node`), cart/order (`IOrderRepository`), pricing, markets, inventory, the `Mediachase.*` foundation
- **Search & Navigation (Find)** — typed `IClient` queries, `FilterBuilder`, `FilterForVisitor`, indexing
- **Migration comprehension** — reading a CMS 11 app well enough to plan a CMS 12 upgrade

## Skill Structure

```
developing-with-optimizely-cms-11/
├── SKILL.md           # Quick reference + troubleshooting matrix
├── REFERENCE.md       # Comprehensive guide + migration comprehension
├── VALIDATION.md      # Coverage matrix + intentional scope boundaries
├── README.md          # This file
├── templates/         # Version-pinned, genericized scaffolds
│   ├── ContentType.template.cs
│   ├── PageController.template.cs
│   ├── PageView.template.cshtml
│   ├── InitializationModule.template.cs
│   ├── ScheduledJob.template.cs
│   ├── CommerceCart.template.cs
│   ├── FindQuery.template.cs
│   ├── packages.config.template
│   ├── web.config.episerver.template
│   └── README.md
└── examples/          # Annotated end-to-end slices
    ├── CmsContentSlice.example.cs
    ├── CommerceCheckout.example.cs
    ├── FindSearch.example.cs
    └── README.md
```

## When to Use Each File

| Need | File |
|------|------|
| Orient fast, look up a symptom → fix | **SKILL.md** |
| Deep dive, full API surface, migration mapping | **REFERENCE.md** |
| Scaffold a content type / controller / job / Commerce / Find | **templates/** |
| See how the tiers connect end-to-end | **examples/** |
| Confirm coverage / scope boundaries | **VALIDATION.md** |

## When This Skill Loads

The CMS 11 fingerprint:

- `packages.config` with `EPiServer.CMS*` / `EPiServer.Framework*` at **11.x** (optionally `EPiServer.Commerce*` 13.x, `EPiServer.Find*` 13.x)
- `[ContentType]` classes deriving from `PageData` / `BlockData`
- `IInitializableModule` / `IConfigurableModule`, `[ScheduledPlugIn]`
- `IContentRepository` / `IContentLoader`; `Html.PropertyFor` in `.cshtml`

## Validated Baseline

| Component | Version |
|-----------|---------|
| Host runtime | .NET Framework 4.6.1 (see `developing-with-dotnet-4x`) |
| CMS (core/framework) | 11.x |
| CMS UI | 11.x |
| Commerce | 13.x |
| Search & Navigation (Find) | 13.x |
| DI | EPiServer.ServiceLocation.StructureMap 2.x / StructureMap 4.x |

> The baseline reflects a representative deployed CMS 11 stack. Treat the exact versions in the project's `packages.config` as the source of truth. These are maintenance baselines, not an endorsement of remaining on the legacy line.

## Authentication

This skill is **content/platform-focused and auth-agnostic**. A CMS 11 site may authenticate via ASP.NET Identity (`EPiServer.CMS.UI.AspNetIdentity` is common), OWIN cookie/OAuth, external OIDC, or another mechanism. The skill notes only where identity *integrates* with Optimizely (virtual roles, content access rights); the mechanism itself is out of scope — bring your own. OWIN middleware ordering lives in `developing-with-dotnet-4x`.

## Related Skills

- **`developing-with-dotnet-4x`** — the host .NET Framework 4.x platform layer (load alongside when host is `v4.x`)
- **developing-with-typescript / developing-with-react** — front-ends that consume Find/Commerce APIs

## Scope Boundaries

Out of scope (by design): the host .NET Framework platform (→ `developing-with-dotnet-4x`); Optimizely CMS 12+/Commerce 14+ and current SaaS (`Optimizely.*` on modern .NET — migration target only); identity/authentication mechanisms; deep Dojo/Shell edit-UI customization; add-ons outside the validated baseline (Forms, Profile Store, Personalization, DXP/CMP cloud). See [VALIDATION.md](VALIDATION.md#scope-boundaries-intentional-exclusions).

## Version

- **Skill Version**: 1.0.0
- **Validated Products**: Optimizely/Episerver CMS 11.x, Commerce 13.x, Find 13.x
- **Validated Host**: .NET Framework 4.6.1 (applicable 4.6.1–4.8)
- **Framing**: Legacy maintenance (debug / modify / migrate)

---

**Status**: Maintenance-focused | **Grade**: Public / generic (no client-specific content)
