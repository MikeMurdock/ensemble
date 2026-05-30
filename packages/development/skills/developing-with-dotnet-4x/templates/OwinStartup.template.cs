// Template: OWIN / Katana Startup — .NET Framework 4.x
// Package baseline: Microsoft.Owin 3.1.0, Microsoft.Owin.Host.SystemWeb 3.1.0,
//                   Microsoft.AspNet.WebApi.Owin 5.2.3 (net461)
// Placeholders: {{Namespace}}
//
// The OwinStartup assembly attribute names the startup type. Host.SystemWeb bridges
// OWIN into the classic IIS pipeline so System.Web and OWIN coexist.
using Owin;
using Microsoft.Owin;
using System.Web.Http;

[assembly: OwinStartup(typeof({{Namespace}}.Startup))]

namespace {{Namespace}}
{
    public class Startup
    {
        // ORDER IS THE PIPELINE. Middleware runs top-to-bottom inbound, unwinds in reverse.
        public void Configuration(IAppBuilder app)
        {
            // 1) OUTERMOST: diagnostics / error handling (must wrap everything below to catch it).
            //    app.UseErrorPage();   // Microsoft.Owin.Diagnostics (dev only)

            // 2) Static files, if served via OWIN.
            //    app.UseStaticFiles();  // Microsoft.Owin.StaticFiles

            // 3) Authentication middleware — MUST run before the framework handler that needs identity.
            //    (cookie / bearer / external). Auth is out of scope for this skill: register your
            //    chosen middleware here, in this position. See SKILL.md "Authentication (Out of Scope)".
            //    e.g. app.UseCookieAuthentication(new CookieAuthenticationOptions { ... });

            // 4) INNERMOST: the framework handler (Web API shown; MVC stays in System.Web).
            var config = new HttpConfiguration();
            WebApiConfig.Register(config);
            app.UseWebApi(config);
        }
    }
}
