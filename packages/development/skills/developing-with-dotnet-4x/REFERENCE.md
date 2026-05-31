# .NET Framework 4.x — Comprehensive Guide

In-depth companion to [SKILL.md](SKILL.md). This guide targets the **maintenance lifecycle** of an existing classic-framework application: understanding what is there, changing it safely, diagnosing the legacy-specific failure modes, and comprehending it well enough to migrate. Validated on **.NET Framework 4.6.1**; the runtime/library APIs are identical across **4.6.1–4.8** (4.6.1 itself is past Microsoft end-of-support — treat version pins as the deployed baseline, not a recommendation to stay there).

## Table of Contents

1. [The Legacy Project System](#1-the-legacy-project-system)
2. [ASP.NET MVC 5](#2-aspnet-mvc-5)
3. [ASP.NET Web API 2](#3-aspnet-web-api-2)
4. [OWIN / Katana](#4-owin--katana)
5. [Entity Framework 6](#5-entity-framework-6)
6. [ADO.NET Data Access](#6-adonet-data-access)
7. [web.config In Depth](#7-webconfig-in-depth)
8. [Assembly Binding & Dependencies](#8-assembly-binding--dependencies)
9. [Testing](#9-testing)
10. [Diagnostics & Troubleshooting](#10-diagnostics--troubleshooting)
11. [Migration Comprehension (Legacy → Modern .NET)](#11-migration-comprehension-legacy--modern-net)
12. [Configuration Reference](#12-configuration-reference)

---

## 1. The Legacy Project System

### 1.1 Old-style (non-SDK) csproj

The classic `.csproj` predates the SDK-style format. Distinguishing traits:

- Root element declares the **MSBuild 2003 namespace**: `xmlns="http://schemas.microsoft.com/developer/msbuild/2003"` and a `ToolsVersion`.
- **No globbing.** Every compiled file is an explicit `<Compile Include="..."/>`; every static asset is `<Content>`/`<None>`/`<EmbeddedResource>`.
- References are explicit `<Reference>` items; NuGet references carry a `<HintPath>` into `..\packages\<id>.<version>\lib\<tfm>\`.
- `<TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>` (the `v` prefix and the leading framework moniker matter).
- `<ProjectTypeGuids>` classify the project for the designer (web app, test, class library).

```xml
<PropertyGroup>
  <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
  <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
  <ProjectGuid>{00000000-0000-0000-0000-000000000000}</ProjectGuid>
  <ProjectTypeGuids>{349c5851-65df-11da-9384-00065b846f21};{fae04ec0-301f-11d3-bf4b-00c04f79efbc}</ProjectTypeGuids>
  <OutputType>Library</OutputType>
  <RootNamespace>Acme.Web</RootNamespace>
  <AssemblyName>Acme.Web</AssemblyName>
  <TargetFrameworkVersion>v4.6.1</TargetFrameworkVersion>
</PropertyGroup>

<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
  <DebugSymbols>true</DebugSymbols>
  <DebugType>full</DebugType>
  <Optimize>false</Optimize>
  <OutputPath>bin\</OutputPath>
  <DefineConstants>DEBUG;TRACE</DefineConstants>
</PropertyGroup>
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
  <DebugType>pdbonly</DebugType>
  <Optimize>true</Optimize>
  <OutputPath>bin\</OutputPath>
  <DefineConstants>TRACE</DefineConstants>
</PropertyGroup>
```

Common project-type GUIDs you will encounter:

| GUID | Meaning |
|------|---------|
| `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}` | C# project |
| `{349C5851-65DF-11DA-9384-00065B846F21}` | ASP.NET Web Application (combined with the C# GUID) |
| `{3AC096D0-A1C2-E12C-1390-A8335801FDAB}` | Test project |

**Maintenance hazards:**
- A new `.cs` file on disk is invisible to the build until it appears as `<Compile Include>`. CI "missing type" errors that don't reproduce in VS are usually this.
- `<ItemGroup>` regions are merge-conflict magnets; resolve by keeping items sorted and unique.
- `bin\`/`obj\` and `packages\` are build output / restored content — never the source of truth.

### 1.2 packages.config & NuGet restore

`packages.config` is a **flat** list of direct *and* transitive packages with exact versions and a target-framework moniker:

```xml
<packages>
  <package id="Microsoft.AspNet.Mvc" version="5.2.3" targetFramework="net461" />
  <package id="Microsoft.AspNet.Razor" version="3.2.3" targetFramework="net461" />
  <package id="Microsoft.AspNet.WebPages" version="3.2.3" targetFramework="net461" />
  <package id="Microsoft.AspNet.Web.Optimization" version="1.1.3" targetFramework="net461" />
  <package id="Microsoft.AspNet.WebApi" version="5.2.3" targetFramework="net461" />
  <package id="EntityFramework" version="6.2.0" targetFramework="net461" />
  <package id="Microsoft.Owin" version="3.1.0" targetFramework="net461" />
  <package id="Microsoft.Owin.Host.SystemWeb" version="3.1.0" targetFramework="net461" />
  <package id="Newtonsoft.Json" version="13.0.3" targetFramework="net461" />
</packages>
```

- There is **no transitive resolution and no lockfile**. The file *is* the closure; adding a package means adding its dependencies too.
- Restore with `nuget.exe restore Solution.sln` (the `packages.config` world, not `dotnet restore`).
- **Version drift across projects is common and dangerous.** When two projects in one solution reference, say, `EntityFramework` 6.0.0 and 6.2.0, the loaded assembly at runtime is governed by binding redirects in the *host* app's `web.config`. Audit with:
  ```bash
  grep -rh 'id="EntityFramework"' --include="packages.config" . | sort | uniq -c
  ```

### 1.3 AssemblyInfo & versioning

Old-style projects keep assembly metadata in `Properties/AssemblyInfo.cs`:

```csharp
[assembly: AssemblyTitle("Acme.Web")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: ComVisible(false)]
[assembly: Guid("...")]
```

`AssemblyVersion` participates in **strong-name identity and binding redirects** — bumping it can force every consumer to add/adjust a redirect. `AssemblyFileVersion` is informational.

### 1.4 Solution organization

A maintainable layered solution typically separates:

```
Acme.Web      (System.Web.Mvc)      -> presentation (controllers, views, App_Start)
Acme.Api      (System.Web.Http)     -> HTTP API surface (OWIN Startup, WebApiConfig)
Acme.Core     (class library)       -> domain models + service interfaces/impl
Acme.Data     (EntityFramework)     -> DbContext, entity configs, Migrations/
Acme.Tests    (MSTest)              -> unit tests (Moq for collaborators)
```

Dependency direction flows inward (Web/Api → Core ← Data). Keep EF6 types behind `Core` interfaces so the web tier never references `System.Data.Entity` directly — this is also what makes the data layer mockable and migration-friendly.

---

## 2. ASP.NET MVC 5

### 2.1 The request lifecycle

`HttpApplication` (Global.asax) → routing (`UrlRoutingModule`) → `MvcHandler` → controller factory → action invoker → action filters → action method → result filters → `ActionResult.ExecuteResult` → view engine (Razor) → response. Knowing this ordering is what lets you place a filter, module, or diagnostic at the right stage.

### 2.2 Routing

Convention routing (in `App_Start/RouteConfig.cs`) plus optional attribute routing (`routes.MapMvcAttributeRoutes()`):

```csharp
public static void RegisterRoutes(RouteCollection routes)
{
    routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
    routes.MapMvcAttributeRoutes();                 // enables [Route] on actions
    routes.MapRoute(
        name: "Default",
        url: "{controller}/{action}/{id}",
        defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional });
}
```

Route order matters: the **first match wins**. Place specific routes before the catch-all default.

### 2.3 Controllers & action results

```csharp
public class ProductsController : Controller
{
    private readonly IProductService _service;
    public ProductsController(IProductService service) => _service = service;

    public ActionResult Index() => View(_service.All());

    public ActionResult Details(int id)
    {
        var product = _service.Find(id);
        if (product == null) return HttpNotFound();
        return View(product);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public ActionResult Create(ProductViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var id = _service.Add(model);
        return RedirectToAction("Details", new { id });
    }
}
```

`ActionResult` subtypes: `ViewResult`, `PartialViewResult`, `RedirectToRouteResult`, `JsonResult`, `ContentResult`, `FileResult`, `HttpStatusCodeResult`, `HttpNotFoundResult`.

> **JSON note (MVC):** `return Json(data, JsonRequestBehavior.AllowGet)` is required for GET in classic MVC, and the default serializer is `JavaScriptSerializer` (not Json.NET). For JSON APIs prefer **Web API 2**.

### 2.4 Filters

Five filter types run in a defined order: **Authorization → Action → Result → Exception** (with `OnActionExecuting`/`OnActionExecuted` bracketing the action). Register globally in `FilterConfig`, or per-controller/action via attributes.

```csharp
public class LogActionFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext c) { /* before */ }
    public override void OnActionExecuted(ActionExecutedContext c)  { /* after  */ }
}
```

### 2.5 Model binding & validation

DataAnnotations drive both client and server validation:

```csharp
public class ProductViewModel
{
    [Required, StringLength(200)]
    public string Name { get; set; }

    [Range(0, 100000)]
    public decimal Price { get; set; }

    [Display(Name = "Category")]
    public int CategoryId { get; set; }
}
```

`ModelState.IsValid` reflects binding + annotation results. Custom binders implement `IModelBinder`; register in `Global.asax` via `ModelBinders.Binders.Add(...)`.

### 2.6 Razor 3 views

```cshtml
@model Acme.Web.Models.ProductViewModel
@{ ViewBag.Title = "Edit Product"; Layout = "~/Views/Shared/_Layout.cshtml"; }

@using (Html.BeginForm())
{
    @Html.AntiForgeryToken()
    @Html.LabelFor(m => m.Name)
    @Html.EditorFor(m => m.Name)
    @Html.ValidationMessageFor(m => m.Name)
    <button type="submit">Save</button>
}
```

- `_ViewStart.cshtml` sets the default `Layout`; `_ViewImports` does **not** exist in MVC 5 (that's Core) — shared `@using` go in `web.config` under `<system.web.webPages.razor>` `<pages><namespaces>`.
- Partial views: `@Html.Partial("_Row", item)`; child actions: `@Html.Action("Widget")`.
- HTML helpers (`@Html.*`) and URL helpers (`@Url.Action`) are the idiom; tag helpers are Core-only.

### 2.7 Bundling & minification (System.Web.Optimization)

```csharp
bundles.Add(new ScriptBundle("~/bundles/app")
    .Include("~/Scripts/jquery-{version}.js", "~/Scripts/app/*.js"));
bundles.Add(new StyleBundle("~/Content/css")
    .Include("~/Content/site.css"));
BundleTable.EnableOptimizations = true;   // force bundling even in Debug
```

Render with `@Scripts.Render("~/bundles/app")` / `@Styles.Render("~/Content/css")`. Bundling is active when `debug="false"` in `<compilation>` or when `EnableOptimizations = true`.

---

## 3. ASP.NET Web API 2

### 3.1 MVC vs Web API — two separate stacks

| Concern | MVC 5 (`System.Web.Mvc`) | Web API 2 (`System.Web.Http`) |
|---------|--------------------------|-------------------------------|
| Base class | `Controller` | `ApiController` |
| Config object | `RouteCollection` / `GlobalFilters` | `HttpConfiguration` |
| Routing | `RouteTable.Routes` | `config.Routes` + `MapHttpAttributeRoutes()` |
| Result | `ActionResult` | `IHttpActionResult` / `HttpResponseMessage` |
| Filters | `System.Web.Mvc` filters | `System.Web.Http.Filters` filters |
| Serialization | `JavaScriptSerializer` | `MediaTypeFormatter` (Json.NET default) |

They can co-host in one web app, but **never share types** — `[Authorize]` from MVC is not `[Authorize]` from Web API.

### 3.2 Routing & content negotiation

```csharp
public static void Register(HttpConfiguration config)
{
    config.MapHttpAttributeRoutes();
    config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}",
        new { id = RouteParameter.Optional });

    config.Formatters.Remove(config.Formatters.XmlFormatter);     // JSON only
    var json = config.Formatters.JsonFormatter.SerializerSettings;
    json.ContractResolver = new CamelCasePropertyNamesContractResolver();
    json.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
}
```

Content negotiation picks a formatter from the `Accept` header; removing the XML formatter forces JSON. Web API uses **Json.NET** by default (unlike classic MVC).

### 3.3 Controllers & results

```csharp
[RoutePrefix("api/products")]
public class ProductsController : ApiController
{
    private readonly IProductService _service;
    public ProductsController(IProductService service) => _service = service;

    [HttpGet, Route("")]
    public IHttpActionResult List() => Ok(_service.All());

    [HttpGet, Route("{id:int}")]
    public IHttpActionResult Get(int id)
    {
        var p = _service.Find(id);
        return p == null ? (IHttpActionResult)NotFound() : Ok(p);
    }

    [HttpPost, Route("")]
    public IHttpActionResult Create([FromBody] ProductDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = _service.Add(dto);
        return Created(new Uri(Request.RequestUri, $"/api/products/{created.Id}"), created);
    }

    [HttpDelete, Route("{id:int}")]
    public IHttpActionResult Delete(int id) { _service.Remove(id); return StatusCode(HttpStatusCode.NoContent); }
}
```

Binding rules: complex types come from the **body** (`[FromBody]`, one per request), simple types from the **route/query** (`[FromUri]`).

### 3.4 Filters, handlers, DI

- **Action/exception filters**: `System.Web.Http.Filters.ActionFilterAttribute`, `IExceptionFilter`. Register globally on `config.Filters`.
- **Message handlers** (`DelegatingHandler`) wrap the entire pipeline (logging, auth pre-checks); add to `config.MessageHandlers`.
- **Dependency injection**: implement `IDependencyResolver` (or use an adapter for your container) and assign `config.DependencyResolver`. Web API resolves controllers through it. (MVC has a separate `System.Web.Mvc.IDependencyResolver`.)

### 3.5 CORS

`Microsoft.AspNet.WebApi.Cors`:

```csharp
config.EnableCors(new EnableCorsAttribute(origins: "https://app.example.com",
                                          headers: "*", methods: "GET,POST,PUT,DELETE"));
```

### 3.6 Hosting

- **Web host** (`Microsoft.AspNet.WebApi.WebHost`): registered via `GlobalConfiguration.Configure(WebApiConfig.Register)` in `Global.asax`; runs in the IIS/`System.Web` pipeline.
- **OWIN host** (`Microsoft.AspNet.WebApi.Owin`): `app.UseWebApi(config)` in `Startup.Configuration`; decoupled from `System.Web`. Don't do both for the same routes.

---

## 4. OWIN / Katana

### 4.1 Concept

OWIN is a specification decoupling .NET web apps from the server. **Katana** is Microsoft's implementation (`Microsoft.Owin.*`). The unit of composition is middleware: `Func<IDictionary<string,object>, Task>` (the "environment"), usually authored against `IAppBuilder`.

### 4.2 Startup discovery

```csharp
[assembly: Microsoft.Owin.OwinStartup(typeof(Acme.Api.Startup))]

public class Startup
{
    public void Configuration(IAppBuilder app) { /* build the pipeline */ }
}
```

Discovery order: the `OwinStartup` attribute → an `appSetting` `owin:appStartup` → a conventionally-named `Startup` class. `Microsoft.Owin.Host.SystemWeb` hooks Katana into the classic IIS pipeline so the same app can serve `System.Web` and OWIN.

### 4.3 Middleware ordering — the cardinal rule

The pipeline executes **in registration order on the way in**, and unwinds in reverse on the way out. Therefore:

```csharp
public void Configuration(IAppBuilder app)
{
    // 1. Outermost: diagnostics / error pages (must wrap everything below to catch it)
    app.UseErrorPage();   // requires the Microsoft.Owin.Diagnostics package (not in the base closure)

    // 2. Static files (short-circuit before app logic)
    // app.UseStaticFiles();   // (Microsoft.Owin.StaticFiles)

    // 3. Authentication middleware — must run BEFORE the handler that needs identity
    //    (cookie / bearer / external) — see Authentication section in SKILL.md

    // 4. Innermost: the framework handler
    var config = new HttpConfiguration();
    WebApiConfig.Register(config);
    app.UseWebApi(config);
}
```

**Failure modes traced to ordering:**
- Auth middleware after `UseWebApi` → `User` is unauthenticated inside controllers.
- Error middleware after a throwing component → unhandled exception escapes to the host.
- Static files after expensive middleware → wasted work on asset requests.

### 4.4 Authoring middleware

```csharp
app.Use(async (ctx, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    await next();                       // call the rest of the pipeline
    ctx.Response.Headers["X-Elapsed-ms"] = sw.ElapsedMilliseconds.ToString();
});
```

For reusable middleware, derive from `OwinMiddleware` and expose an `IAppBuilder` extension `app.UseXxx()`.

---

## 5. Entity Framework 6

### 5.1 DbContext lifetime

`DbContext` is a **unit of work + identity map**; it is *not* thread-safe and should be **short-lived** — one per web request (or per logical operation), created and disposed within that scope. Long-lived contexts accumulate tracked entities and stale data.

```csharp
public class AcmeContext : DbContext
{
    public AcmeContext() : base("name=DefaultConnection") { }   // resolves the named connection string
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
}
```

`"name=DefaultConnection"` *requires* the connection string to exist (throws if missing); `"DefaultConnection"` (without `name=`) would fall back to treating it as a literal connection string / database name. Prefer `name=`.

### 5.2 Code-first vs database-first

- **Code-first**: entities + `OnModelCreating`/Fluent API or DataAnnotations define the schema; migrations evolve it.
- **Database-first / EDMX**: an `.edmx` designer model generated from an existing database (older style). Maintenance-only; the EF6 tooling still supports it but it is effectively frozen.
- **Code-first from an existing database**: scaffolds POCOs + a context without an `.edmx`.

Identify which a project uses by presence of `*.edmx` (database-first) vs a `Migrations/` folder (code-first).

### 5.3 Fluent configuration

```csharp
protected override void OnModelCreating(DbModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(e =>
    {
        e.ToTable("Products");
        e.HasKey(p => p.Id);
        e.Property(p => p.Name).IsRequired().HasMaxLength(200);
        e.Property(p => p.Price).HasPrecision(18, 2);
        e.HasRequired(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId);
    });
    // or: modelBuilder.Configurations.Add(new ProductConfiguration());  // EntityTypeConfiguration<T>
}
```

### 5.4 Migrations in depth

```powershell
Enable-Migrations                          # creates Migrations/Configuration.cs (once)
Add-Migration AddProductPrice              # diff model vs last snapshot -> Up()/Down()
Update-Database                            # apply pending
Update-Database -Script                    # emit SQL (for DBA review / deploy)
Update-Database -TargetMigration:"AddProductTable"   # roll forward/back to a point
Add-Migration <Name> -IgnoreChanges        # baseline an existing DB without scaffolding ops
```

```csharp
internal sealed class Configuration : DbMigrationsConfiguration<AcmeContext>
{
    public Configuration()
    {
        AutomaticMigrationsEnabled = false;          // explicit migrations only (recommended)
        ContextKey = "Acme.Data.AcmeContext";
    }
    protected override void Seed(AcmeContext context)
    {
        context.Categories.AddOrUpdate(c => c.Name, new Category { Name = "Default" });
    }
}
```

- Each migration is a class with `Up()`/`Down()` and a designer snapshot of the model at that point.
- Applied migrations are recorded in **`__MigrationHistory`** (with the model hash). To know the deployed schema state, query that table — do not assume the latest migration is applied.
- **Initializers** control startup behavior: `MigrateDatabaseToLatestVersion<TContext,TConfig>`, `CreateDatabaseIfNotExists`, `DropCreateDatabaseIfModelChanges` (dev only). Set via `Database.SetInitializer(...)` or `<entityFramework><contexts>` in config.

### 5.5 Querying & async

```csharp
using (var db = new AcmeContext())
{
    var page = await db.Products
        .Where(p => p.Price > 0)
        .OrderBy(p => p.Name)
        .Include(p => p.Category)               // eager load to avoid N+1
        .Skip(skip).Take(take)
        .AsNoTracking()                         // read-only => skip change tracking
        .ToListAsync();
}
```

- `Include` (eager) vs lazy loading (virtual nav props + proxies) — lazy loading causes **N+1** and fails on disposed contexts.
- `AsNoTracking()` for read paths; it materially reduces overhead.
- EF6 async exists but composes over ADO.NET async; **never** `.Result`/`.Wait()` it on a request thread (see [§10](#10-diagnostics--troubleshooting)).

### 5.6 Transactions & resiliency

```csharp
using (var tx = db.Database.BeginTransaction())
{
    try { /* ... */ db.SaveChanges(); tx.Commit(); }
    catch { tx.Rollback(); throw; }
}
```

Connection resiliency via `SetExecutionStrategy` (`SqlAzureExecutionStrategy`) retries transient failures; note it is **incompatible with user-initiated transactions** (`Database.BeginTransaction` / `TransactionScope`). The documented workaround is to **suspend the execution strategy** for that operation (a connection factory that returns a non-retrying strategy when a flag is set) and drive the transaction yourself inside a single `executionStrategy.Execute(() => { ... })` call, so the whole unit retries atomically.

---

## 6. ADO.NET Data Access

When EF6 is too heavy (bulk, reporting, stored-proc-centric code), `System.Data.SqlClient` is the raw path.

```csharp
public IEnumerable<Product> FindByCategory(int categoryId)
{
    const string sql = "SELECT Id, Name, Price FROM Products WHERE CategoryId = @cat";
    using (var conn = new SqlConnection(_connectionString))
    using (var cmd = new SqlCommand(sql, conn))
    {
        cmd.Parameters.Add("@cat", SqlDbType.Int).Value = categoryId;   // typed + parameterized
        conn.Open();
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                yield return new Product { Id = r.GetInt32(0), Name = r.GetString(1), Price = r.GetDecimal(2) };
    }
}
```

- Always wrap `SqlConnection`/`SqlCommand`/`SqlDataReader` in `using` (deterministic disposal returns connections to the pool).
- **Parameterize everything** — never concatenate user input into SQL.
- Stored procedures: `cmd.CommandType = CommandType.StoredProcedure`.
- `System.Data.SqlClient` (4.x in-box / the 4.x NuGet `System.Data.SqlClient`) is the classic provider; `Microsoft.Data.SqlClient` is the modern successor (a migration target, not used here).

---

## 7. web.config In Depth

### 7.1 Section map

```
configuration
├── configSections            # declares custom/section handlers (entityFramework, etc.)
├── appSettings               # key/value app config
├── connectionStrings         # named connections + providerName
├── system.web                # ASP.NET: compilation, httpRuntime, authentication, customErrors, sessionState, httpModules
├── system.webServer          # IIS7+ integrated pipeline: modules, handlers, security, rewrite
├── entityFramework           # EF6 provider + default connection factory
├── runtime/assemblyBinding   # dependentAssembly bindingRedirects
└── location path="..."       # scoped overrides for a sub-path
```

### 7.2 Pipelines: `system.web` vs `system.webServer`

- `system.web/httpModules` + `httpHandlers` = **Classic** IIS pipeline (legacy).
- `system.webServer/modules` + `handlers` = **Integrated** pipeline (IIS7+, the norm). Modules here run for all managed requests when `runAllManagedModulesForAllRequests="true"`.
- A module registered only in `system.web` will not run under the Integrated pipeline — duplicate or migrate registrations accordingly.

### 7.3 `<location>` scoping

```xml
<location path="admin">
  <system.web>
    <authorization><deny users="?" /></authorization>
  </system.web>
</location>
```

Applies settings to a sub-path only. `inheritInChildApplications="false"` prevents a child app from inheriting a parent's settings (common source of "works at root, breaks in virtual dir").

### 7.4 Transforms (XDT)

`Web.<Configuration>.config` transforms apply during **publish/package** (MSBuild `TransformXml`), keyed by build configuration. The transform namespace is `xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform"`.

```xml
<configuration xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform">
  <system.web>
    <compilation xdt:Transform="RemoveAttributes(debug)" />
    <customErrors mode="On" xdt:Transform="Replace" />
  </system.web>
  <appSettings>
    <add key="ClientCacheMinutes" value="1440" xdt:Transform="SetAttributes" xdt:Locator="Match(key)" />
  </appSettings>
</configuration>
```

Transform verbs: `Replace`, `Insert`, `InsertBefore/After`, `Remove`, `RemoveAll`, `SetAttributes`, `RemoveAttributes`. Locators: `Match(attr)`, `Condition(xpath)`, `XPath(...)`.

> **Local F5 does not transform.** To preview, `msbuild /t:TransformXml` or publish to a folder and inspect the output `web.config`.

### 7.5 Protecting secrets

`aspnet_regiis -pe "connectionStrings"` encrypts a section with DPAPI/RSA at rest. (Modern guidance moves secrets to Key Vault / environment — a migration concern; in-place, section encryption is the classic answer.)

---

## 8. Assembly Binding & Dependencies

The **single most common legacy failure class.**

### 8.1 Why redirects exist

The CLR resolves strong-named assemblies by **exact identity** (name + version + culture + public key token). When project A wants `Newtonsoft.Json` 11.0.0 but the restored/loaded version is 13.0.0, load fails unless a `bindingRedirect` tells the CLR to substitute:

```xml
<runtime>
  <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
    <dependentAssembly>
      <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0" />
    </dependentAssembly>
  </assemblyBinding>
</runtime>
```

A real `web.config` may carry **hundreds** of these (one per diverging strong-named dependency). They are normal — but stale or missing ones break the app at runtime, not build time.

### 8.2 Generating & repairing

- `<AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>` (app.config; works for exe/test hosts).
- For web apps, NuGet writes redirects on install. Repair drift with:
  - PM> `Add-BindingRedirect` (computes correct redirects from the resolved graph)
  - PM> `Update-Package -reinstall <id>` (re-applies a package and its redirects)
- After any dependency bump, **verify** the redirect upper bound (`oldVersion="0.0.0.0-<new>"`) covers the new version.

### 8.3 Diagnosing load failures (Fusion log)

When you hit `FileLoadException`/`FileNotFoundException`/manifest-mismatch:
1. Read the exception's assembly identity (the version it *wanted*).
2. Enable the **Fusion assembly binding log** (`fuslogvw.exe`, or registry `EnableLog`) to see probing paths and the chosen version.
3. Add/fix the matching `bindingRedirect`, or align the package versions across projects.

### 8.4 The GAC

Strong-named, machine-shared assemblies can live in the Global Assembly Cache. Legacy apps occasionally depend on GAC'd assemblies (e.g. older system components); these resolve before `bin\`. Prefer `bin\`-deployed (xcopy-able) dependencies for portability.

---

## 9. Testing

### 9.1 MSTest structure

```csharp
[TestClass]
public class ProductServiceTests
{
    private Mock<IProductRepository> _repo;
    private ProductService _sut;

    [ClassInitialize] public static void ClassInit(TestContext _) { /* once per class */ }
    [TestInitialize]  public void Init()    { _repo = new Mock<IProductRepository>(); _sut = new ProductService(_repo.Object); }
    [TestCleanup]     public void Cleanup() { /* per test */ }

    [TestMethod]
    public void Add_Persists_AndReturnsId()
    {
        _repo.Setup(r => r.Insert(It.IsAny<Product>())).Returns(7);
        var id = _sut.Add(new ProductDto { Name = "Widget", Price = 9.99m });
        Assert.AreEqual(7, id);
        _repo.Verify(r => r.Insert(It.Is<Product>(p => p.Name == "Widget")), Times.Once);
    }

    [DataTestMethod]
    [DataRow(0), DataRow(-5)]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void Find_Rejects_NonPositiveId(int id) => _sut.Find(id);
}
```

Attribute lifecycle: `[AssemblyInitialize]` → `[ClassInitialize]` → (`[TestInitialize]` → `[TestMethod]` → `[TestCleanup]`)* → `[ClassCleanup]`. Assertions via `Assert`, `CollectionAssert`, `StringAssert`. Packages: `MSTest.TestAdapter` + `MSTest.TestFramework` — this is **MSTest V2** (the NuGet-delivered framework, `net45`+), not the pre-V2 in-box `Microsoft.VisualStudio.QualityTools.UnitTestFramework`.

### 9.2 Moq idioms

```csharp
_repo.Setup(r => r.Get(It.IsAny<int>())).Returns<int>(id => new Product { Id = id });
_repo.Setup(r => r.GetAsync(It.IsAny<int>())).ReturnsAsync(new Product());        // async
_repo.SetupSequence(r => r.Next()).Returns(1).Returns(2).Throws<InvalidOperationException>();
_repo.Verify(r => r.Save(), Times.Never);
var mock = new Mock<IProductRepository>(MockBehavior.Strict);                      // fail on unexpected calls
```

### 9.3 Testing controllers

- **Web API**: instantiate the `ApiController` directly with mocked services; assert on `IHttpActionResult` (`result as OkNegotiatedContentResult<T>`). For `Request`/`Url` dependencies, set `controller.Request = new HttpRequestMessage()` and `controller.Configuration = new HttpConfiguration()`.
- **MVC**: instantiate the `Controller`; assert the returned `ActionResult` type (`Assert.IsInstanceOfType(result, typeof(ViewResult))`) and its `Model`.

### 9.4 Testing the data layer

Keep EF6 behind a repository interface and mock it for unit tests. For integration coverage, run against **LocalDB**/a disposable SQL database with migrations applied (`Update-Database`), then assert through the real `DbContext`. Avoid mocking `DbSet` directly — it is brittle; prefer the repository seam.

> **xUnit note:** Many 4.x solutions also use **xUnit 2.x + Moq** instead of MSTest (`[Fact]`/`[Theory]`/`[InlineData]`, constructor for setup, `IDisposable` for teardown). The patterns map 1:1; this skill features MSTest because it is in-box and dependency-light, but xUnit is equally valid.

---

## 10. Diagnostics & Troubleshooting

### 10.1 Full matrix

| Symptom | Likely cause | Diagnosis | Fix |
|---------|--------------|-----------|-----|
| `Could not load file or assembly … manifest definition does not match` | Binding mismatch | Read wanted version; Fusion log | Add/repair `bindingRedirect` ([§8](#8-assembly-binding--dependencies)) |
| Request hangs / thread-pool starvation | Sync-over-async deadlock | Stacks show `WaitOne`/`GetResult` on a request thread | Async all the way; `ConfigureAwait(false)` in libs |
| Web API route 404 | Attribute routes not mapped / wrong order | Check `MapHttpAttributeRoutes()` + `Application_Start` wiring | Map attribute routes before convention; ensure `GlobalConfiguration.Configure` runs |
| OWIN auth/diagnostic "ignored" | Middleware order | Inspect `Startup.Configuration` ordering | Reorder `app.Use…` (outer→inner) |
| Prod config wrong, local OK | XDT transform not applied locally | Inspect published `web.config` | Build/publish with target config; verify locators |
| `The model backing the context has changed` | EF6 model ≠ DB | Query `__MigrationHistory`; compare model hash | `Add-Migration` delta or correct initializer |
| `500.19` on IIS | Bad/locked config section | IIS detailed error; check `applicationHost.config` overrides | Unlock section / fix `system.webServer` |
| Missing `..\packages\…props` | No restore | Build log | `nuget restore` |
| Type missing on CI only | File not in csproj `<Compile>` | diff csproj vs disk | Add `<Compile Include>` |
| Slow page, many queries | EF6 lazy loading N+1 | SQL profiler / EF logging | `Include(...)` + `AsNoTracking()` |

### 10.2 Anatomy of the sync-over-async deadlock

In classic ASP.NET, the request runs on a thread with a single-threaded `AspNetSynchronizationContext`. Blocking that thread on an async operation (`task.Result`) prevents the continuation — which needs to resume *on that same context* — from ever running. Deadlock.

```csharp
// DEADLOCK in a request:
var data = _service.GetAsync().Result;

// CORRECT:
var data = await _service.GetAsync();           // controller action is async
// In library code that must stay sync-callable, isolate with ConfigureAwait(false)
// throughout the async chain so continuations don't capture the request context.
```

### 10.3 EF6 query logging

```csharp
db.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);   // logs generated SQL + timings
```

Use this to confirm N+1 patterns and to see the actual SQL EF emits.

### 10.4 Server-side diagnostics

- `customErrors mode="Off"` (temporarily, non-prod) to see real exceptions.
- `<system.web><trace enabled="true"/>` or ETW/Event Log for handler-level failures.
- Application performance monitoring hooks (e.g. an APM agent module in `system.webServer/modules`) are common in legacy apps — treat the agent as opaque infra.

---

## 11. Migration Comprehension (Legacy → Modern .NET)

This skill owns the **source side**: reading a 4.x app accurately enough to migrate it. A modern-.NET skill owns the target. The goal here is *comprehension and mapping*, not rewriting.

### 11.1 What maps to what

| Legacy (.NET Framework 4.x) | Modern .NET (target) | Migration note |
|------------------------------|----------------------|----------------|
| Old-style csproj + `packages.config` | SDK-style csproj + `<PackageReference>` | `packages.config` → PackageReference is a mechanical first step; unlocks transitive restore |
| `web.config` (XML) | `appsettings.json` + `IConfiguration` | Connection strings/app settings port; `system.web`/`webServer` concepts are replaced by middleware/host config |
| `Global.asax` `Application_Start` | `Program.cs` (minimal hosting) | Composition root moves to the host builder |
| `App_Start/*Config.cs` | `Program.cs` service/middleware registration | RouteConfig/FilterConfig/BundleConfig collapse into builder calls (bundling → a build-time bundler) |
| OWIN `Startup.Configuration(IAppBuilder)` | ASP.NET Core middleware (`app.Use…`) | Conceptually the closest mapping; ordering rules carry over |
| `System.Web.Mvc` `Controller` | ASP.NET Core MVC `Controller` | Razor views mostly port; HTML helpers → tag helpers; `_ViewImports.cshtml` replaces config-based namespaces |
| `System.Web.Http` `ApiController` | ASP.NET Core `ControllerBase` + `[ApiController]` | `IHttpActionResult` → `IActionResult`/`ActionResult<T>` |
| EF6 (`System.Data.Entity`) | EF Core | **Not** a drop-in: different APIs, migration format, query translation, and behaviors (no lazy-loading by default, different `DbContext` config) |
| `System.Data.SqlClient` | `Microsoft.Data.SqlClient` | Namespace/package swap; mostly source-compatible |
| MSTest/xUnit on 4.x | Same frameworks on modern .NET | Test code ports with minimal change |
| Binding redirects | (none) | SDK-style + unified deps eliminate most redirects |

### 11.2 Reading an app for migration

1. **Inventory the target framework and packages** (`grep TargetFrameworkVersion`, audit `packages.config`). Note version drift and any package without a modern equivalent.
2. **Map the composition root** — what `Application_Start` and `Startup.Configuration` register, in order. This becomes the modern host pipeline.
3. **Find `System.Web` coupling** — `HttpContext.Current`, `Server.MapPath`, `Request`/`Response` access, `HttpModules`. These are the hard parts; they need the Core equivalents (`IHttpContextAccessor`, `IWebHostEnvironment`, middleware).
4. **Isolate EF6** — confirm code-first vs EDMX, read `__MigrationHistory`, catalog entities/relationships. EF6→EF Core is a re-platform, not a port.
5. **Identify third-party/platform lock-in** (any large framework layered on the app, identity stack) — these gate the migration and may need their own modern equivalents.

### 11.3 Incremental strategy

- Extract framework-agnostic logic into **.NET Standard 2.0** libraries that *both* the legacy app and the new app can reference. This shrinks the eventual cutover.
- Strangler-fig: stand up the modern app alongside, route endpoints over incrementally (reverse proxy / YARP on the target side).
- Migrate `packages.config` → `PackageReference` and bump to a still-supported 4.x (4.8) **before** attempting a cross-runtime move — it de-risks the dependency graph first.

---

## 12. Configuration Reference

### 12.1 Validated stack versions (baseline)

| Component | Package | Version (baseline) |
|-----------|---------|--------------------|
| Runtime | .NET Framework | 4.6.1 (APIs identical 4.6.1–4.8) |
| MVC | `Microsoft.AspNet.Mvc` | 5.2.3 |
| Razor | `Microsoft.AspNet.Razor` | 3.2.3 |
| Web Pages | `Microsoft.AspNet.WebPages` | 3.2.3 |
| Bundling | `Microsoft.AspNet.Web.Optimization` | 1.1.3 |
| Web API | `Microsoft.AspNet.WebApi` (+ Core/Client/WebHost/Owin) | 5.2.3 |
| ORM | `EntityFramework` | 6.2.0 (watch for 6.0.0/6.1.3 drift) |
| OWIN host | `Microsoft.Owin` / `Microsoft.Owin.Host.SystemWeb` | 3.1.0 |
| JSON | `Newtonsoft.Json` | 13.0.3 (latest patched, `net461`-compatible; legacy baselines often shipped 10.x–11.x) |
| Testing | `MSTest.TestAdapter` / `MSTest.TestFramework` (MSTest V2) | 1.2.0 |
| Mocking | `Moq` | 4.7.x–4.8.x |

> Pins reflect a representative deployed baseline. For any dependency with known CVEs (notably JSON/serialization libraries), prefer the latest patched version that still targets `net46x`/`net48` and re-verify binding redirects.

### 12.2 Target framework monikers

| `TargetFrameworkVersion` | NuGet TFM | `targetFramework` (packages.config) |
|--------------------------|-----------|--------------------------------------|
| `v4.6.1` | `net461` | `net461` |
| `v4.7.2` | `net472` | `net472` |
| `v4.8`   | `net48`  | `net48`  |

### 12.3 See Also

- [SKILL.md](SKILL.md) — quick reference and decision tables
- [templates/](templates/) — version-pinned scaffolds
- [examples/](examples/) — annotated end-to-end examples
- [VALIDATION.md](VALIDATION.md) — coverage matrix and scope boundaries
