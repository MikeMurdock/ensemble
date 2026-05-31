# .NET Framework 4.x Development Skill

Maintenance patterns for **legacy ASP.NET applications on the classic .NET Framework** — the kind of long-lived enterprise web app still running on MVC 5, Web API 2, Entity Framework 6, and OWIN. This skill helps you **understand, debug, modify, and migrate** existing 4.x apps.

> **Not a greenfield skill.** New applications should target modern .NET. This skill deliberately covers the *legacy* stack and serves as the **source side** for migrations — modern .NET is the target.

## Overview

Comprehensive guidance for classic-framework maintenance, focused on:

- **The old-style project system** — non-SDK csproj, `packages.config`, NuGet restore, binding redirects
- **ASP.NET MVC 5** — App_Start, Global.asax, controllers, Razor 3, bundling
- **ASP.NET Web API 2** — `ApiController`, attribute routing, content negotiation, DI
- **OWIN / Katana** — `Startup` discovery and the cardinal middleware-ordering rule
- **Entity Framework 6** — code-first, migrations, `__MigrationHistory`, query/async caveats
- **web.config** — sections, transforms (XDT), and the Integrated vs Classic pipeline
- **Legacy debugging** — binding redirects, sync-over-async deadlocks, transform gotchas, EF6 model drift
- **Migration comprehension** — reading a 4.x app accurately enough to move it forward

## Skill Structure

```
developing-with-dotnet-4x/
├── SKILL.md           # Quick reference + troubleshooting matrix
├── REFERENCE.md       # Comprehensive guide + migration comprehension
├── VALIDATION.md      # Coverage matrix + intentional scope boundaries
├── README.md          # This file
├── templates/         # Version-pinned, genericized scaffolds
│   ├── WebApiController.template.cs
│   ├── MvcController.template.cs
│   ├── DbContext.template.cs
│   ├── OwinStartup.template.cs
│   ├── WebApiConfig.template.cs
│   ├── MSTest.template.cs
│   ├── packages.config.template
│   ├── web.config.template
│   ├── csproj.template.xml
│   └── README.md
└── examples/          # Annotated end-to-end slices
    ├── ProductApi.example.cs
    ├── OwinPipeline.example.cs
    └── README.md
```

## When to Use Each File

| Need | File |
|------|------|
| Orient fast, look up a symptom → fix | **SKILL.md** |
| Deep dive, full troubleshooting, migration mapping | **REFERENCE.md** |
| Scaffold a controller / context / config | **templates/** |
| See how the tiers connect end-to-end | **examples/** |
| Confirm coverage / scope boundaries | **VALIDATION.md** |

## When This Skill Loads

The classic-framework fingerprint:

- Old-style `*.csproj` with `<TargetFrameworkVersion>v4.x</TargetFrameworkVersion>`
- `packages.config`, `web.config`, `Global.asax`, `App_Start/`
- References to `System.Web.Mvc`, `System.Web.Http`, `EntityFramework`, `Microsoft.Owin.*`

## Validated Baseline

| Component | Version |
|-----------|---------|
| .NET Framework | 4.6.1 (APIs identical 4.6.1–4.8) |
| ASP.NET MVC | 5.2.3 |
| ASP.NET Web API | 5.2.3 |
| Razor | 3.2.3 |
| Web Optimization | 1.1.3 |
| Entity Framework | 6.2.0 |
| OWIN (Microsoft.Owin) | 3.1.0 |
| MSTest | 1.2.0 |

> The baseline reflects a representative deployed enterprise stack. 4.6.1 is past Microsoft end-of-support; pins are the maintenance baseline, not an endorsement of staying there. For dependencies with known CVEs (notably JSON/serialization), upgrade to the latest patched `net46x`/`net48` build and re-verify binding redirects.

## Authentication

This skill is **platform-focused and auth-agnostic**. A legacy 4.x app may use any identity stack (ASP.NET Identity, OWIN cookie/OAuth, external OIDC, IdentityServer/Duende, Azure AD). The skill notes only where identity *integrates* with the platform (OWIN middleware ordering); the identity mechanism itself is out of scope. Bring your own — this skill imposes nothing.

## Related Skills

- **developing-with-typescript / developing-with-react** — front-ends that consume these APIs
- **developing-with-python / nestjs** — sibling backend skills (rich-layout reference)

## Scope Boundaries

Out of scope (by design): modern .NET / .NET Core, third-party platforms/frameworks layered on the app, identity-provider internals, WCF/WebForms. See [VALIDATION.md](VALIDATION.md#scope-boundaries-intentional-exclusions).

## Version

- **Skill Version**: 1.0.0
- **Validated Framework**: .NET Framework 4.6.1 (applicable 4.6.1–4.8)
- **Framing**: Legacy maintenance (debug / modify / migrate)

---

**Status**: Maintenance-focused | **Grade**: Public / generic (no client-specific content)
