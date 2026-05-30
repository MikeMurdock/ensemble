# .NET Framework 4.x Skill — Validation Report

**Status**: Maintenance-focused | **Validated baseline**: .NET Framework 4.6.1 (APIs identical 4.6.1–4.8)
**Framing**: legacy maintenance — debug / modify / migrate existing apps (not greenfield)

---

## Feature Parity Matrix

### Project System

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Old-style (non-SDK) csproj | Yes | SKILL §3, REFERENCE §1, templates/csproj.template.xml | MSBuild 2003 ns, explicit items |
| packages.config | Yes | SKILL §3, REFERENCE §1.2, templates/packages.config.template | Flat closure, no lockfile |
| NuGet restore | Yes | SKILL §Quick Start, REFERENCE §1.2 | `nuget restore` (not `dotnet restore`) |
| AssemblyInfo & versioning | Yes | REFERENCE §1.3 | Strong-name identity impact |
| Solution layering | Yes | REFERENCE §1.4 | Web/Api/Core/Data/Tests |

### ASP.NET MVC 5

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Routing (convention + attribute) | Yes | SKILL §4, REFERENCE §2.2 | RouteConfig, MapMvcAttributeRoutes |
| Controllers & ActionResult | Yes | SKILL §4, REFERENCE §2.3, templates/MvcController.template.cs | View/Redirect/Json/HttpNotFound |
| App_Start config classes | Yes | SKILL §4, REFERENCE §2.x | Route/Filter/Bundle config |
| Global.asax lifecycle | Yes | SKILL §4, REFERENCE §2.1 | Composition root + ordering |
| Filters | Yes | REFERENCE §2.4 | Auth/Action/Result/Exception |
| Model binding & validation | Yes | REFERENCE §2.5 | DataAnnotations, ModelState |
| Razor 3 views | Yes | REFERENCE §2.6 | _ViewStart, helpers (no tag helpers) |
| Bundling (Web.Optimization) | Yes | SKILL §4, REFERENCE §2.7 | Script/StyleBundle |

### ASP.NET Web API 2

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| MVC vs Web API stack separation | Yes | SKILL §5, REFERENCE §3.1 | Distinct namespaces/types |
| WebApiConfig + routing | Yes | SKILL §5, REFERENCE §3.2, templates/WebApiConfig.template.cs | Attribute + convention |
| ApiController + IHttpActionResult | Yes | SKILL §5, REFERENCE §3.3, templates/WebApiController.template.cs | Ok/NotFound/Created |
| Content negotiation / formatters | Yes | REFERENCE §3.2 | JSON-first via Json.NET |
| Filters & message handlers | Yes | REFERENCE §3.4 | DelegatingHandler |
| Dependency injection | Yes | REFERENCE §3.4 | IDependencyResolver |
| CORS | Yes | REFERENCE §3.5 | EnableCorsAttribute |
| Hosting (WebHost vs OWIN) | Yes | REFERENCE §3.6 | Co-host caveats |

### OWIN / Katana

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Startup discovery | Yes | SKILL §6, REFERENCE §4.2, templates/OwinStartup.template.cs | OwinStartup attribute |
| Middleware ordering | Yes | SKILL §6/§10, REFERENCE §4.3, examples/OwinPipeline.example.cs | The cardinal rule |
| Authoring middleware | Yes | REFERENCE §4.4 | app.Use + next() |
| Host.SystemWeb bridge | Yes | REFERENCE §4.2 | IIS integration |

### Entity Framework 6

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| DbContext lifetime | Yes | REFERENCE §5.1 | Per-request/unit-of-work |
| Code-first + Fluent config | Yes | SKILL §7, REFERENCE §5.3, templates/DbContext.template.cs | OnModelCreating |
| Migrations (Add/Update/Script) | Yes | SKILL §7, REFERENCE §5.4 | __MigrationHistory, Seed |
| Code-first vs EDMX | Yes | REFERENCE §5.2 | Identify which a project uses |
| Querying & async caveats | Yes | REFERENCE §5.5 | Include, AsNoTracking, N+1 |
| Transactions & resiliency | Yes | REFERENCE §5.6 | BeginTransaction, retry strategy |
| ADO.NET fallback | Yes | SKILL §7, REFERENCE §6 | SqlConnection, parameterized |

### web.config & Configuration

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Section map | Yes | SKILL §8, REFERENCE §7.1 | configSections..runtime |
| system.web vs system.webServer | Yes | REFERENCE §7.2 | Classic vs Integrated pipeline |
| `<location>` scoping | Yes | REFERENCE §7.3 | inheritInChildApplications |
| Transforms (XDT) | Yes | SKILL §8, REFERENCE §7.4, templates/web.config.template | Debug/Release, not F5 |
| Secret protection | Partial | REFERENCE §7.5 | aspnet_regiis (migration → vault) |

### Assembly Binding

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Binding redirects | Yes | SKILL §10, REFERENCE §8.1 | The #1 legacy failure |
| Generating/repairing | Yes | REFERENCE §8.2 | Add-BindingRedirect, reinstall |
| Fusion log diagnosis | Yes | REFERENCE §8.3 | fuslogvw |
| GAC | Yes | REFERENCE §8.4 | bin\ preference |

### Testing

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| MSTest structure | Yes | SKILL §9, REFERENCE §9.1, templates/MSTest.template.cs | Lifecycle attributes |
| Moq idioms | Yes | SKILL §9, REFERENCE §9.2 | Setup/Returns/Verify |
| Testing controllers | Yes | REFERENCE §9.3 | MVC + Web API |
| Testing the data layer | Yes | REFERENCE §9.4 | Repository seam, LocalDB |
| xUnit alternative | Noted | REFERENCE §9.4 | 1:1 mapping noted |

### Legacy Debugging

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Troubleshooting matrix | Yes | SKILL §10, REFERENCE §10.1 | Symptom → cause → fix |
| Sync-over-async deadlock | Yes | SKILL §10/§11, REFERENCE §10.2 | SynchronizationContext anatomy |
| EF6 query logging | Yes | REFERENCE §10.3 | Database.Log |
| Server diagnostics | Yes | REFERENCE §10.4 | customErrors, IIS 500.19 |

### Migration Comprehension

| Feature | Covered | Location | Notes |
|---------|---------|----------|-------|
| Legacy → modern mapping table | Yes | REFERENCE §11.1 | This skill = source side |
| Reading an app for migration | Yes | REFERENCE §11.2 | System.Web coupling, EF6 isolation |
| Incremental strategy | Yes | REFERENCE §11.3 | .NET Standard, strangler-fig |

---

## Template Coverage

| Template | Purpose | Placeholders | Status |
|----------|---------|--------------|--------|
| WebApiController.template.cs | Web API 2 CRUD | Namespace, Entity, entity, entities | Complete |
| MvcController.template.cs | MVC 5 controller | Namespace, Entity, entity | Complete |
| DbContext.template.cs | EF6 code-first + migrations | Namespace, Context, Entity | Complete |
| OwinStartup.template.cs | OWIN pipeline | Namespace | Complete |
| WebApiConfig.template.cs | App_Start config | Namespace | Complete |
| MSTest.template.cs | MSTest + Moq | Namespace, Sut, Dependency | Complete |
| packages.config.template | Pinned dependency closure | — | Complete |
| web.config.template | Load-bearing config sections | — (literal neutral domain) | Complete |
| csproj.template.xml | Old-style web project | ProjectName, ProjectGuid | Complete |

## Example Coverage

| Example | Patterns Demonstrated | Status |
|---------|----------------------|--------|
| ProductApi.example.cs | DTO ↔ controller ↔ service ↔ repository ↔ EF6 | Complete |
| OwinPipeline.example.cs | Middleware ordering + failure modes | Complete |

---

## Validation Checklist

### Documentation Quality
- [x] SKILL.md provides a quick reference with decision tables
- [x] REFERENCE.md provides a comprehensive guide + troubleshooting matrix
- [x] All code examples are syntactically valid C# / XML for .NET Framework 4.x
- [x] Versions are pinned to a validated baseline (net461)
- [x] Legacy-maintenance framing is consistent (not greenfield)

### Confidentiality (public-grade)
- [x] No client identifiers, repo names, domains, or internal component names
- [x] All examples use neutral `Acme.*` / `Product` domain
- [x] Content derived from canonical Microsoft framework patterns, not copied source
- [x] Leak-scanned before commit

### Template & Example Quality
- [x] Templates use consistent placeholder conventions
- [x] Templates are version-pinned and immediately adaptable
- [x] Examples are self-documenting and demonstrate real maintenance patterns

---

## Scope Boundaries (Intentional Exclusions)

| Topic | Why excluded | Where it belongs |
|-------|--------------|------------------|
| Modern .NET / .NET Core / .NET 5+ | Different project system, hosting, DI, config | A modern-.NET skill (this skill is the migration *source* only) |
| CMS / DXP platforms (e.g. Optimizely/EPiServer, Sitecore) | Platform-specific init modules, content model, APIs | A platform-specific skill |
| Identity providers (ASP.NET Identity internals, IdentityServer/Duende, OIDC, Azure AD) | Auth-agnostic by design | A dedicated identity skill |
| WCF / WebForms | Distinct legacy stacks | Separate legacy skills if needed |
| Front-end frameworks / SPA build | Out of scope for the server platform | Front-end skills |

---

## Recommendations

### For Skill Users
1. **Start with SKILL.md** for orientation and the troubleshooting matrix.
2. **Consult REFERENCE.md** for deep dives and migration comprehension.
3. **Copy templates** as version-pinned starting points; bump versions + binding redirects together.
4. **Read examples** to see how the tiers connect before refactoring.

### For Skill Maintainers
1. Update the version baseline table (REFERENCE §12.1) as you validate newer 4.x targets.
2. Keep the troubleshooting matrix synced between SKILL and REFERENCE.
3. Re-run a leak scan for client identifiers (org/product names, domains, repo and internal component names) before any commit — examples must stay on the neutral `Acme.*` / `Product` domain.
4. Add migration mappings as the companion modern-.NET skill evolves.
