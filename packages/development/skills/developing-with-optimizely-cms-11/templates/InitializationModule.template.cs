// Template: Optimizely/Episerver CMS 11 initialization + DI module — EPiServer.Framework 11.x on .NET Framework 4.x
// Packages: EPiServer.Framework 11.x, EPiServer.ServiceLocation.StructureMap 2.x (net461)
// Placeholders: {{Namespace}}
//
// Two module flavors shown:
//   * IConfigurableModule  -> ConfigureContainer registers services BEFORE the container is built.
//   * IInitializableModule -> Initialize/Uninitialize for wiring (e.g. content event subscriptions).
// Module run order is controlled by [ModuleDependency], NOT declaration order.
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace {{Namespace}}.Infrastructure
{
    [InitializableModule]
    public class ContainerInitialization : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            // Optimizely's DI (StructureMap-backed). Prefer constructor injection in consumers.
            context.Services.AddTransient<IExampleService, ExampleService>();
            context.Services.AddSingleton<IExampleCache, ExampleCache>();
        }

        public void Initialize(InitializationEngine context) { }
        public void Uninitialize(InitializationEngine context) { }
    }

    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]   // ensure CMS is initialized first
    public class EventsInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var events = context.Locate.Advanced.GetInstance<IContentEvents>();
            events.PublishedContent += OnPublishedContent;
        }

        public void Uninitialize(InitializationEngine context)
        {
            var events = context.Locate.Advanced.GetInstance<IContentEvents>();
            events.PublishedContent -= OnPublishedContent;   // ALWAYS unsubscribe
        }

        private void OnPublishedContent(object sender, ContentEventArgs e)
        {
            // React to publish (e.g. clear a custom cache, enqueue an index update).
        }
    }

    public interface IExampleService { void Do(); }
    public class ExampleService : IExampleService { public void Do() { } }
    public interface IExampleCache { }
    public class ExampleCache : IExampleCache { }
}
