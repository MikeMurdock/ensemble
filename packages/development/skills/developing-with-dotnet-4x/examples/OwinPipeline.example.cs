// =============================================================================
// Example: OWIN / Katana middleware pipeline ordering on .NET Framework 4.6.1
// -----------------------------------------------------------------------------
// The #1 OWIN maintenance bug is wrong middleware ORDER. This example makes the
// ordering rule concrete with inline middleware and explains what breaks when
// each piece is out of place. Genericized; auth is shown only as a positional
// placeholder (this skill is auth-agnostic — see SKILL.md).
//
// Package baseline (packages.config, net461):
//   Microsoft.Owin 3.1.0, Microsoft.Owin.Host.SystemWeb 3.1.0,
//   Microsoft.AspNet.WebApi.Owin 5.2.3
// =============================================================================
using System.Diagnostics;
using System.Web.Http;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Acme.Api.Example.OwinPipelineStartup))]

namespace Acme.Api.Example
{
    public class OwinPipelineStartup
    {
        // The pipeline runs TOP-TO-BOTTOM inbound, and UNWINDS bottom-to-top outbound.
        // Each app.Use(...) wraps everything registered after it.
        public void Configuration(IAppBuilder app)
        {
            // (1) OUTERMOST — correlation / timing / catch-all.
            //     Registered first => wraps all later middleware, so it can time the
            //     whole request and observe exceptions bubbling out of inner stages.
            app.Use(async (ctx, next) =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    await next();   // <-- invoke the rest of the pipeline
                }
                finally
                {
                    sw.Stop();
                    ctx.Response.Headers["X-Elapsed-ms"] = sw.ElapsedMilliseconds.ToString();
                }
            });

            // (2) ERROR HANDLING — must sit ABOVE the components that can throw.
            //     If you register this AFTER UseWebApi, exceptions thrown while
            //     building the response have already escaped: it never runs.
            app.Use(async (ctx, next) =>
            {
                try { await next(); }
                catch (System.Exception ex)
                {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync($"{{\"error\":\"{ex.GetType().Name}\"}}");
                }
            });

            // (3) STATIC FILES would go here (short-circuit asset requests before app logic).
            //     app.UseStaticFiles();   // Microsoft.Owin.StaticFiles

            // (4) AUTHENTICATION — positional placeholder ONLY.
            //     Auth MUST run before the framework handler that reads identity, or
            //     controllers see an unauthenticated User. Register your chosen
            //     middleware HERE. This skill imposes no identity stack.
            //     e.g. app.UseCookieAuthentication(new CookieAuthenticationOptions { ... });

            // (5) INNERMOST — the framework handler. Runs last inbound; its response
            //     unwinds back up through (2) error handling and (1) timing.
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}",
                new { id = RouteParameter.Optional });
            app.UseWebApi(config);   // Microsoft.AspNet.WebApi.Owin
        }
    }

    // -------------------------------------------------------------------------
    // ORDERING FAILURE MODES (what to look for when maintaining a pipeline):
    //
    //   * Auth middleware placed AFTER UseWebApi  -> User is never authenticated
    //                                                in controllers (401/anonymous).
    //   * Error middleware placed AFTER UseWebApi -> unhandled exceptions escape
    //                                                to the host (yellow screen / 500).
    //   * Static files placed AFTER expensive
    //     middleware                              -> CPU wasted on asset requests.
    //   * Timing middleware not OUTERMOST         -> measured time excludes stages
    //                                                registered before it.
    // -------------------------------------------------------------------------
}
