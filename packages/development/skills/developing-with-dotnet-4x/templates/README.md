# .NET Framework 4.x Templates

Version-pinned, genericized scaffolds for maintaining legacy .NET Framework 4.x applications. All templates use a neutral `Acme.*` namespace and a `Product`/`Category` domain — replace the placeholders below for your project.

## Available Templates

### 1. WebApiController.template.cs
**ASP.NET Web API 2 controller** (`System.Web.Http`, `ApiController`) with full CRUD returning `IHttpActionResult` and attribute routing.
**Placeholders**: `{{Namespace}}`, `{{Entity}}`, `{{entity}}`, `{{entities}}`

### 2. MvcController.template.cs
**ASP.NET MVC 5 controller** (`System.Web.Mvc`, `Controller`) with `ActionResult` actions, anti-forgery, and `ModelState` validation.
**Placeholders**: `{{Namespace}}`, `{{Entity}}`, `{{entity}}`

### 3. DbContext.template.cs
**Entity Framework 6 code-first** `DbContext` + entities + Fluent config + `DbMigrationsConfiguration` (with PM console setup commands).
**Placeholders**: `{{Namespace}}`, `{{Context}}`, `{{Entity}}`

### 4. OwinStartup.template.cs
**OWIN / Katana `Startup`** with the `OwinStartup` assembly attribute and the canonical, **order-annotated** middleware pipeline (error → static → auth → framework handler).
**Placeholders**: `{{Namespace}}`

### 5. WebApiConfig.template.cs
**`App_Start/WebApiConfig.cs`** — attribute + convention routing, JSON-first formatter setup, DI resolver hook.
**Placeholders**: `{{Namespace}}`

### 6. MSTest.template.cs
**MSTest test class with Moq** — `[TestClass]`/`[TestInitialize]`/`[TestMethod]`, `[DataTestMethod]`/`[DataRow]`, and `Setup`/`Verify`.
**Placeholders**: `{{Namespace}}`, `{{Sut}}`, `{{Dependency}}`

### 7. packages.config.template
**`packages.config`** for an MVC 5 + Web API 2 + EF6 + OWIN web app, with the validated version baseline and a current Json.NET pin.

### 8. web.config.template
**Minimal-but-complete `web.config`** — `connectionStrings`, `entityFramework`, `system.web`/`system.webServer`, and `runtime/assemblyBinding` redirects.
**Placeholders**: none — uses a literal neutral domain (`Acme`); read and adapt directly.

### 9. csproj.template.xml
**Old-style (non-SDK) web project** — MSBuild 2003 namespace, explicit `<Compile>`/`<Reference>` items, `<HintPath>` into `..\packages\`, and the `packages.config` restore guard.
**Placeholders**: `{{ProjectName}}`, `{{ProjectGuid}}`

## Placeholder Conventions

| Placeholder | Meaning | Example |
|-------------|---------|---------|
| `{{Namespace}}` | Root namespace | `Acme.Api` |
| `{{Entity}}` | Entity, PascalCase singular | `Product` |
| `{{entity}}` | Entity, lower singular | `product` |
| `{{entities}}` | Entity, lower plural (routes) | `products` |
| `{{Context}}` | EF6 DbContext name | `AcmeContext` |
| `{{Sut}}` | System under test | `ProductService` |
| `{{Dependency}}` | Mocked collaborator interface | `IProductRepository` |
| `{{ProjectName}}` | Assembly / root namespace | `Acme.Web` |
| `{{ProjectGuid}}` | Fresh project GUID | `D3F1...` |

## Usage

```bash
# Copy and substitute (macOS/BSD sed shown; use sed -i on GNU)
cp templates/WebApiController.template.cs Controllers/ProductsController.cs
sed -i '' 's/{{Namespace}}/Acme.Api/g; s/{{Entity}}/Product/g; s/{{entity}}/product/g; s/{{entities}}/products/g' Controllers/ProductsController.cs
```

> These reflect the validated baseline (MVC 5.2.3 / Web API 5.2.3 / EF6 6.2.0 / OWIN 3.1.0 on `net461`). Bump versions and binding redirects together; see [REFERENCE.md §8](../REFERENCE.md#8-assembly-binding--dependencies).

## Related Documentation

- [SKILL.md](../SKILL.md) — quick reference
- [REFERENCE.md](../REFERENCE.md) — comprehensive guide
- [examples/](../examples/) — annotated end-to-end examples
