---
name: developing-with-dotnet-4x
description: Legacy .NET Framework 4.x maintenance (validated on 4.6.1) — ASP.NET MVC 5, Web API 2, Razor 3, Entity Framework 6, OWIN/Katana, old-style csproj + packages.config + web.config. Use when debugging, modifying, or migrating EXISTING .NET Framework applications. NOT for greenfield work or modern .NET / .NET Core (use a modern-.NET skill for those).
---

# .NET Framework 4.x Development Skill

Maintenance patterns for **legacy ASP.NET applications on the classic .NET Framework** (validated on 4.6.1; APIs are identical across 4.6.1–4.8). Covers ASP.NET MVC 5, Web API 2, Razor 3, Entity Framework 6, OWIN/Katana, and the old-style MSBuild project system (`packages.config` + `web.config`).

> **This is a legacy-maintenance skill, not a greenfield one.** Use it to *understand, debug, modify, and migrate existing* .NET Framework apps. New applications should target modern .NET — see the migration notes in [REFERENCE.md](REFERENCE.md), which treats this skill as the **source side** (comprehension) and a modern-.NET skill as the target side.

**Progressive Disclosure**: This file is the quick reference. For comprehensive guides, the full troubleshooting matrix, and migration comprehension, see [REFERENCE.md](REFERENCE.md).

## Table of Contents

1. [When to Use](#when-to-use)
2. [Quick Start](#quick-start)
3. [Solution & Project Structure](#solution--project-structure)
4. [ASP.NET MVC 5](#aspnet-mvc-5)
5. [ASP.NET Web API 2](#aspnet-web-api-2)
6. [OWIN / Katana Startup](#owin--katana-startup)
7. [Entity Framework 6](#entity-framework-6)
8. [web.config](#webconfig)
9. [Testing (MSTest + Moq)](#testing-mstest--moq)
10. [Legacy Debugging & Gotchas](#legacy-debugging--gotchas)
11. [Anti-Patterns](#anti-patterns)
12. [CLI Commands](#cli-commands)
13. [Configuration](#configuration)
14. [Authentication (Out of Scope)](#authentication-out-of-scope)
15. [See Also](#see-also)

---

## When to Use

Loaded by `backend-developer` (or invoked directly) when a repository shows the **classic framework fingerprint**:

- Old-style (non-SDK) `*.csproj` with `<TargetFrameworkVersion>v4.x</TargetFrameworkVersion>` (e.g. `v4.6.1`, `v4.7.2`, `v4.8`)
- `packages.config` alongside projects (NuGet restore, not `<PackageReference>`)
- `web.config` (not `appsettings.json`), `Global.asax`, `App_Start/` folders
- References to `System.Web.Mvc`, `System.Web.Http`, `EntityFramework`, `Microsoft.Owin.*`

**Scope (in):** .NET Framework 4.x, ASP.NET MVC 5.2.x, Web API 2 (5.2.x), Razor 3.2.x, EF6 (6.x), OWIN/Katana (Microsoft.Owin 3.x), System.Web.Optimization (bundling), ADO.NET / `System.Data.SqlClient`, MSTest + Moq, old-style csproj + `packages.config` + `web.config`/transforms.

**Scope (out — use a dedicated skill):**
- **Modern .NET / .NET Core / .NET 5+** — different project system (SDK-style), hosting model (Kestrel/`Program.cs`), DI, and config. This skill is the *legacy source* for migrations only.
- **CMS / DXP platforms** (e.g. Optimizely/EPiServer, Sitecore) — a legacy app may host one, but its initialization modules, content model, and APIs belong in a platform-specific skill. Treat platform types as opaque here.
- **Identity providers** (ASP.NET Identity internals, IdentityServer/Duende, external OIDC, Azure AD) — see [Authentication (Out of Scope)](#authentication-out-of-scope).

---

## Quick Start

### Orient in an unfamiliar legacy solution

```bash
# 1. What framework version(s)? (expect v4.x across the board)
grep -rh "TargetFrameworkVersion" --include="*.csproj" .

# 2. SDK-style or old-style? (old-style = no "Sdk=" attribute on <Project>)
grep -rl 'Project Sdk=' --include="*.csproj" .   # empty result => all old-style

# 3. What does each project pull in? (versions are your source of truth)
grep -rh 'id="Microsoft.AspNet\|id="EntityFramework\|id="Microsoft.Owin' --include="packages.config" . | sort | uniq -c

# 4. Restore packages BEFORE opening in an IDE or building
nuget restore Solution.sln          # or: msbuild /t:restore (packages.config era: nuget restore)
```

### Minimal Web API 2 controller

```csharp
using System.Net;
using System.Web.Http;

namespace Acme.Api.Controllers
{
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service) => _service = service;

        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            var product = _service.Find(id);
            return product == null
                ? (IHttpActionResult)NotFound()
                : Ok(product);
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create(ProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = _service.Add(dto);
            return Created($"api/products/{created.Id}", created);
        }
    }
}
```

### Minimal MVC 5 controller

```csharp
using System.Web.Mvc;

namespace Acme.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Submit(ContactViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            // ... handle ...
            return RedirectToAction("Index");
        }
    }
}
```

> **More**: full controllers, DI wiring, and DTOs in [templates/](templates/) and [examples/](examples/).

---

## Solution & Project Structure

### Old-style (non-SDK) csproj anatomy

Legacy projects use the **MSBuild 2003 XML namespace** and enumerate every file explicitly. Unlike SDK-style projects, nothing is globbed.

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" DefaultTargets="Build"
         xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"
          Condition="Exists('...Microsoft.Common.props')" />
  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <ProjectGuid>{GUID-HERE}</ProjectGuid>
    <!-- Web Application + C# project type GUIDs -->
    <ProjectTypeGuids>{349c5851-65df-11da-9384-00065b846f21};{fae04ec0-301f-11d3-bf4b-00c04f79efbc}</ProjectTypeGuids>
    <OutputType>Library</OutputType>
    <RootNamespace>Acme.Web</RootNamespace>
    <AssemblyName>Acme.Web</AssemblyName>
    <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System.Web" />
    <Reference Include="System.Web.Mvc, Version=5.2.3.0, ...">
      <HintPath>..\packages\Microsoft.AspNet.Mvc.5.2.3\lib\net45\System.Web.Mvc.dll</HintPath>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <Compile Include="Controllers\HomeController.cs" />   <!-- every file listed -->
    <Content Include="Web.config" />
  </ItemGroup>
  <Import Project="$(MSBuildBinPath)\Microsoft.CSharp.targets" />
  <!-- NuGet restore guard for packages.config -->
  <Target Name="EnsureNuGetPackageBuildImports" BeforeTargets="PrepareForBuild"> ... </Target>
</Project>
```

**Key facts that bite you:**
- Adding a file on disk does **not** add it to the build — it must appear as `<Compile>`/`<Content>`. Merge conflicts in csproj `<ItemGroup>`s are common.
- References resolve via `<HintPath>` into `..\packages\<id>.<version>\lib\<tfm>\`. If that folder is missing, **restore first**.
- `ProjectTypeGuids` determine designer behavior (Web App vs Class Library vs Test).

### packages.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="Microsoft.AspNet.Mvc" version="5.2.3" targetFramework="net461" />
  <package id="EntityFramework" version="6.2.0" targetFramework="net461" />
  <package id="Microsoft.Owin" version="3.1.0" targetFramework="net461" />
</packages>
```

`packages.config` lists **direct + transitive** packages flatly with exact versions and the `targetFramework` moniker (`net461` = .NET Framework 4.6.1). There is no lockfile and no transitive resolution — what you see is what restores.

### Typical solution layout

```
Solution.sln
├── Acme.Web/            # ASP.NET MVC 5 web app (Global.asax, App_Start/, Views/, Web.config)
├── Acme.Api/            # ASP.NET Web API 2 (App_Start/WebApiConfig.cs, Startup.cs)
├── Acme.Data/           # EF6 DbContext + Migrations/ + entities
├── Acme.Core/           # Domain/services (class library)
├── Acme.Tests/          # MSTest unit tests
└── packages/            # NuGet restore target (git-ignored, restored on demand)
```

> **More**: full csproj/packages.config templates in [templates/](templates/).

---

## ASP.NET MVC 5

### App_Start configuration classes

Classic MVC wires cross-cutting config in `App_Start/*.cs`, invoked from `Global.asax`.

```csharp
// App_Start/RouteConfig.cs
public class RouteConfig
{
    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
        routes.MapRoute(
            name: "Default",
            url: "{controller}/{action}/{id}",
            defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional });
    }
}

// App_Start/FilterConfig.cs
public class FilterConfig
{
    public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        => filters.Add(new HandleErrorAttribute());
}

// App_Start/BundleConfig.cs  (System.Web.Optimization)
public class BundleConfig
{
    public static void RegisterBundles(BundleCollection bundles)
    {
        bundles.Add(new ScriptBundle("~/bundles/jquery").Include("~/Scripts/jquery-{version}.js"));
        bundles.Add(new StyleBundle("~/Content/css").Include("~/Content/site.css"));
        BundleTable.EnableOptimizations = true; // bundling/minification in Release
    }
}
```

### Global.asax — the composition root

```csharp
// Global.asax.cs
public class MvcApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        AreaRegistration.RegisterAllAreas();
        GlobalConfiguration.Configure(WebApiConfig.Register);   // if Web API is co-hosted
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
    }
}
```

> **Order matters.** `RegisterAllAreas()` and `GlobalConfiguration.Configure(WebApiConfig.Register)` run before route registration; getting this wrong yields 404s or attribute-route failures.

### Controllers, action results, Razor

| Concern | Classic MVC 5 |
|---------|---------------|
| Base class | `Controller` (`System.Web.Mvc`) |
| Return type | `ActionResult` — `View()`, `RedirectToAction()`, `Json()`, `HttpNotFound()`, `Content()` |
| Verbs | `[HttpGet]`, `[HttpPost]`; CSRF via `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` |
| Model binding | Action parameters / view models; validate with `ModelState.IsValid` + DataAnnotations |
| Views | Razor 3 `.cshtml`; `_ViewStart.cshtml` sets `Layout`; `@model`, `@Html`, `@Url` helpers |

---

## ASP.NET Web API 2

`System.Web.Http` is a **separate stack** from MVC (`System.Web.Mvc`) — different base class (`ApiController`), different routing (`HttpConfiguration`/`config.Routes`), different filters and message handlers. Don't mix the namespaces.

```csharp
// App_Start/WebApiConfig.cs
public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        config.MapHttpAttributeRoutes();                 // enables [Route]/[RoutePrefix]
        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional });

        // JSON-first: drop XML, camelCase
        config.Formatters.Remove(config.Formatters.XmlFormatter);
        config.Formatters.JsonFormatter.SerializerSettings.ContractResolver =
            new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    }
}
```

- Return `IHttpActionResult` (`Ok()`, `NotFound()`, `BadRequest()`, `Created()`) — testable and status-correct.
- Hosted under IIS via `Microsoft.AspNet.WebApi.WebHost`, or under OWIN via `Microsoft.AspNet.WebApi.Owin` (`app.UseWebApi(config)`).
- Register in `Global.asax` with `GlobalConfiguration.Configure(WebApiConfig.Register)`.

---

## OWIN / Katana Startup

OWIN decouples the app from IIS via a `Startup` class. `Microsoft.Owin.Host.SystemWeb` bridges OWIN into the classic IIS pipeline; the `OwinStartup` assembly attribute names the startup type.

```csharp
[assembly: Microsoft.Owin.OwinStartup(typeof(Acme.Api.Startup))]

namespace Acme.Api
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // ORDER IS THE PIPELINE. Middleware runs top-to-bottom on the way in.
            // 1) diagnostics / error handling first
            // 2) static files
            // 3) authentication middleware (if any — see Authentication section)
            // 4) framework handlers (Web API / MVC) LAST

            var config = new HttpConfiguration();
            WebApiConfig.Register(config);
            app.UseWebApi(config);   // requires Microsoft.AspNet.WebApi.Owin
        }
    }
}
```

> **The classic OWIN bug:** middleware is ordered. Authentication registered *after* the framework handler never runs for those requests; error/diagnostic middleware registered *after* a throwing component never sees the exception. See [Legacy Debugging](#legacy-debugging--gotchas).

---

## Entity Framework 6

`System.Data.Entity` — the classic, **synchronous-friendly** ORM (distinct from EF Core). Async methods exist (`ToListAsync`, `SaveChangesAsync`) but the provider model, migrations, and config are EF6-specific.

### DbContext + code-first

```csharp
using System.Data.Entity;

public class AcmeContext : DbContext
{
    public AcmeContext() : base("name=DefaultConnection") { }   // matches web.config

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().Property(p => p.Name).IsRequired().HasMaxLength(200);
        modelBuilder.Entity<Product>()
            .HasRequired(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId);
    }
}
```

### Code-first migrations (Package Manager Console)

```powershell
Enable-Migrations                       # one-time; creates Migrations/Configuration.cs
Add-Migration AddProductTable           # scaffolds an Up()/Down() migration
Update-Database                         # applies pending migrations
Update-Database -Script                 # generate idempotent SQL instead of applying
Update-Database -TargetMigration:"Init" # roll back to a named migration
```

`Migrations/Configuration.cs` (`DbMigrationsConfiguration<AcmeContext>`) controls `AutomaticMigrationsEnabled` and `Seed()`. Migration history lives in the `__MigrationHistory` table — **read it to know what schema the deployed DB is actually at** before changing the model.

### ADO.NET fallback (`System.Data.SqlClient`)

```csharp
using (var conn = new SqlConnection(connectionString))
using (var cmd = new SqlCommand("SELECT Name FROM Products WHERE Id = @id", conn))
{
    cmd.Parameters.AddWithValue("@id", id);   // always parameterize
    conn.Open();
    using (var reader = cmd.ExecuteReader())
        while (reader.Read()) { /* reader.GetString(0) */ }
}
```

> **More**: DbContext + migration + repository templates in [templates/](templates/); a full data-access example in [examples/](examples/).

---

## web.config

The single XML config file controlling the app. Sections that matter for maintenance:

```xml
<configuration>
  <configSections>
    <section name="entityFramework" type="System.Data.Entity.Internal.ConfigFile.EntityFrameworkSection, EntityFramework, Version=6.0.0.0, ..." />
  </configSections>

  <connectionStrings>
    <add name="DefaultConnection"
         connectionString="Server=.;Database=Acme;Integrated Security=True"
         providerName="System.Data.SqlClient" />
  </connectionStrings>

  <appSettings>
    <add key="webpages:Version" value="3.0.0.0" />
    <add key="ClientCacheMinutes" value="30" />
  </appSettings>

  <system.web>
    <compilation debug="true" targetFramework="4.6.1" />
    <httpRuntime targetFramework="4.6.1" maxRequestLength="40960" />
    <customErrors mode="RemoteOnly" defaultRedirect="~/Error" />
  </system.web>

  <system.webServer>
    <modules runAllManagedModulesForAllRequests="true" />
    <handlers> <!-- e.g. ExtensionlessUrlHandler --> </handlers>
  </system.webServer>

  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" />
        <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
```

### Transforms (Web.Debug.config / Web.Release.config)

XML Document Transform (XDT) files mutate `web.config` **at publish time** (MSBuild `TransformXml`), not at local F5 debug.

```xml
<configuration xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform">
  <system.web>
    <compilation xdt:Transform="RemoveAttributes(debug)" />
  </system.web>
  <connectionStrings>
    <add name="DefaultConnection" connectionString="...prod..."
         xdt:Transform="SetAttributes" xdt:Locator="Match(name)" />
  </connectionStrings>
</configuration>
```

> Transforms apply on **Publish/Build with a configuration**, not when you press F5. "It works locally but the prod connection string is wrong" is almost always a transform issue.

---

## Testing (MSTest + Moq)

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

[TestClass]
public class ProductServiceTests
{
    private Mock<IProductRepository> _repo;
    private ProductService _sut;

    [TestInitialize]
    public void Setup()
    {
        _repo = new Mock<IProductRepository>();
        _sut = new ProductService(_repo.Object);
    }

    [TestMethod]
    public void Find_ReturnsNull_WhenMissing()
    {
        _repo.Setup(r => r.Get(42)).Returns((Product)null);
        Assert.IsNull(_sut.Find(42));
        _repo.Verify(r => r.Get(42), Times.Once);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Find_Throws_OnNonPositiveId(int id) => _sut.Find(id);
}
```

- Test runner: `vstest.console.exe` / `dotnet vstest` against built `*.dll`, or VS Test Explorer. Requires `MSTest.TestAdapter` + `MSTest.TestFramework` packages.
- `[TestInitialize]`/`[TestCleanup]` per-test; `[ClassInitialize]`/`[AssemblyInitialize]` once.
- Mock dependencies with **Moq** (`Setup`/`Returns`/`Verify`). Keep EF6 behind a repository interface so it can be mocked.

---

## Legacy Debugging & Gotchas

The reason this skill exists. The high-frequency failure modes when maintaining 4.x apps:

| Symptom | Root cause | Fix |
|---------|-----------|-----|
| `Could not load file or assembly 'X' Version=… manifest definition does not match` | Assembly version mismatch (transitive dep upgraded) | Add/repair `<bindingRedirect>` in `web.config` `<runtime>`. Regenerate via `Add-BindingRedirect` or `Update-Package -reinstall`. |
| Request **hangs/deadlocks** under load | `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` on async in the classic `AspNetSynchronizationContext` | Go async end-to-end **or** `ConfigureAwait(false)` in library code. Never block on async in a request thread. |
| `404` on Web API attribute routes | `config.MapHttpAttributeRoutes()` missing, or `GlobalConfiguration.Configure` not called | Ensure `WebApiConfig.Register` runs in `Application_Start` and maps attribute routes before convention routes. |
| Auth/diagnostic middleware "not running" | OWIN middleware registered in the wrong order | Reorder `app.Use…` in `Startup.Configuration` — pipeline is top-to-bottom. |
| Prod config wrong though local works | XDT transform not applied (F5 doesn't transform) | Build/publish with the target configuration; verify `Web.Release.config` locators (`Match(name)`). |
| `The model backing the context has changed` (EF6) | DB schema ≠ model snapshot | Reconcile `__MigrationHistory`; `Add-Migration` the delta or set the correct initializer. |
| Build error: missing `..\packages\…\*.props` | NuGet packages not restored | `nuget restore Solution.sln` before build/open. |
| `HTTP 500.19` / config errors on IIS | Locked/duplicate config sections, or missing IIS feature | Check `<location>` overrides and `system.webServer` against the host's `applicationHost.config`. |
| File compiles locally, missing in build server | File not added as `<Compile>` in csproj | Add the `<Compile Include>` item; don't rely on on-disk presence. |

> **More**: the full troubleshooting matrix with diagnostic steps is in [REFERENCE.md](REFERENCE.md#10-diagnostics--troubleshooting).

---

## Anti-Patterns

| Anti-Pattern | Problem | Fix |
|--------------|---------|-----|
| `.Result` / `.Wait()` on async in a request | Deadlock via captured `SynchronizationContext` | Async all the way; `ConfigureAwait(false)` in libraries |
| Mixing `System.Web.Mvc` & `System.Web.Http` types | Wrong base/filters/routing; subtle breakage | Keep MVC and Web API stacks separate |
| New `DbContext` per call without `using` | Connection leaks, change-tracker bloat | One `DbContext` per request/unit-of-work, disposed |
| Hand-editing binding redirects ad hoc | Drift, runtime load failures | Let NuGet generate/repair them; keep `<runtime>` consistent |
| String-concatenated SQL | SQL injection | Parameterize (`SqlParameter` / EF LINQ) |
| Treating greenfield .NET advice as applicable | SDK-style/Core APIs don't exist in 4.x | Use 4.x-specific patterns; reach for a modern-.NET skill only when migrating |
| Catch-all `catch (Exception)` swallowing | Hides binding/config failures | Catch specific types; log with context |

---

## CLI Commands

```bash
# Restore (packages.config era — use nuget.exe, not `dotnet restore`)
nuget restore Solution.sln

# Build (Windows: MSBuild from VS or Build Tools)
msbuild Solution.sln /p:Configuration=Release /p:Platform="Any CPU"

# Run tests (built test assemblies)
vstest.console.exe Acme.Tests\bin\Release\Acme.Tests.dll
#   or in newer toolchains: dotnet vstest Acme.Tests\bin\Release\Acme.Tests.dll

# Publish with web.config transform applied
msbuild Acme.Web\Acme.Web.csproj /t:Package /p:Configuration=Release

# EF6 migrations (Package Manager Console in Visual Studio)
Enable-Migrations ; Add-Migration <Name> ; Update-Database

# Repair binding redirects across a project
#   PM> Add-BindingRedirect
#   PM> Update-Package -reinstall   (heavier hammer; re-applies all packages)
```

> Most of this toolchain is **Windows + MSBuild + IIS/IIS Express**. `dotnet build` can build many old-style projects, but restore/packaging semantics differ from `packages.config` expectations.

---

## Configuration

### web.config essentials checklist

```xml
<!-- 1. Framework pinned consistently -->
<system.web>
  <compilation targetFramework="4.6.1" />
  <httpRuntime targetFramework="4.6.1" />
</system.web>

<!-- 2. Connection string + provider -->
<connectionStrings>
  <add name="DefaultConnection" connectionString="..." providerName="System.Data.SqlClient" />
</connectionStrings>

<!-- 3. EF provider services (when EF6 is present) -->
<entityFramework>
  <defaultConnectionFactory type="System.Data.Entity.Infrastructure.SqlConnectionFactory, EntityFramework" />
  <providers>
    <provider invariantName="System.Data.SqlClient"
              type="System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer" />
  </providers>
</entityFramework>

<!-- 4. Binding redirects under <runtime> for every diverging assembly -->
```

> **More config** (membership, session, custom sections, machineKey, `<location>` scoping): [REFERENCE.md §12](REFERENCE.md#12-configuration-reference).

---

## Authentication (Out of Scope)

**This skill is platform-focused and auth-agnostic.** Authentication and identity are deliberately *not* prescribed here, because a legacy 4.x app can use any of: ASP.NET Identity 2.x, Forms/Windows auth, OWIN cookie/OAuth middleware, an external OIDC provider, IdentityServer/Duende, or Azure AD.

What this skill *does* note:
- `authentication mode="None"` in `web.config` usually means auth is delegated to **OWIN middleware** (registered in `Startup.Configuration`) — its ordering relative to your handlers is a [debugging concern](#owin--katana-startup) covered here.
- Where the identity *mechanism* itself is the subject (token issuance, user stores, claims transformation), use a dedicated identity skill. Bring your own; this skill imposes nothing.

---

## See Also

- **[REFERENCE.md](REFERENCE.md)** — Comprehensive guide: deep dives, full troubleshooting matrix, migration comprehension (legacy → modern .NET).
- **[templates/](templates/)** — Genericized, version-pinned scaffolds (controllers, DbContext, OWIN Startup, csproj, packages.config, web.config, MSTest).
- **[examples/](examples/)** — Annotated end-to-end examples (Web API + EF6 data access, OWIN pipeline ordering).
- **[VALIDATION.md](VALIDATION.md)** — Coverage matrix and intentional scope boundaries.
