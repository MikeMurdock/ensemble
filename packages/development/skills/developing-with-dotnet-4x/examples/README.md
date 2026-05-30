# .NET Framework 4.x Examples

Annotated, end-to-end examples for legacy .NET Framework 4.x maintenance. Genericized (`Acme.*` / `Product` domain) and self-documenting — read them to understand how the layers connect, then adapt the patterns to your codebase.

## Available Examples

### 1. ProductApi.example.cs

**Full Web API 2 + EF6 vertical slice.**

Demonstrates how the layers wire together in a classic app:

- DTO as the API contract (decoupled from the EF entity), with DataAnnotations validation
- `ApiController` returning `IHttpActionResult` (`Ok`/`NotFound`/`BadRequest`/`Created`)
- Service layer doing DTO ↔ entity mapping and domain guards
- Repository seam over an EF6 `DbContext` (with `AsNoTracking`, `Find`, `SaveChanges`)
- Constructor injection throughout (the shape a DI resolver fills)

**Use when**: adding or refactoring an HTTP endpoint backed by EF6 in a legacy app, or learning how the tiers are meant to depend on each other.

### 2. OwinPipeline.example.cs

**OWIN / Katana middleware ordering.**

Makes the cardinal OWIN rule — *order is the pipeline* — concrete:

- Inline middleware for timing and error handling, with `await next()` shown explicitly
- The canonical order: outermost diagnostics → error handling → static files → authentication (positional placeholder) → framework handler
- A failure-mode key documenting exactly what breaks when each piece is misplaced

**Use when**: debugging "my auth/error middleware doesn't run," or reviewing/repairing a `Startup.Configuration` pipeline.

## How to Use These Examples

These files are **illustrative single-file slices** — in a real solution the types live in separate projects (`Acme.Api` / `Acme.Core` / `Acme.Data`). They are written to be read top-to-bottom; the comments carry the lesson.

```bash
# Read the slice
sed -n '1,40p' examples/ProductApi.example.cs

# Lift a layer into your project and split across the right assemblies,
# then replace the Acme.* namespace and Product domain with yours.
```

> They are not wired into a buildable solution here (no csproj/restore). To compile, drop the types into projects that reference the packages in [../templates/packages.config.template](../templates/packages.config.template) and restore.

## Related Documentation

- [SKILL.md](../SKILL.md) — quick reference
- [REFERENCE.md](../REFERENCE.md) — comprehensive guide
- [templates/](../templates/) — version-pinned scaffolds
