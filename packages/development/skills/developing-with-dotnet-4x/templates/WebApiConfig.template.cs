// Template: App_Start/WebApiConfig.cs — ASP.NET Web API 2 configuration — .NET Framework 4.x
// Package baseline: Microsoft.AspNet.WebApi 5.2.3, Newtonsoft.Json (pin current 13.x) (net461)
// Placeholders: {{Namespace}}
//
// Invoke from Global.asax (web host):   GlobalConfiguration.Configure(WebApiConfig.Register);
// or from OWIN Startup:                 WebApiConfig.Register(config); app.UseWebApi(config);
using System.Web.Http;
using Newtonsoft.Json.Serialization;

namespace {{Namespace}}
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Attribute routing ([Route]/[RoutePrefix]) — map BEFORE convention routes.
            config.MapHttpAttributeRoutes();

            // Convention fallback.
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });

            // JSON-first: remove XML, camelCase property names, ignore reference loops.
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            var json = config.Formatters.JsonFormatter.SerializerSettings;
            json.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

            // Dependency injection: assign your container adapter here.
            // config.DependencyResolver = new MyContainerResolver(container);
        }
    }
}
