// =============================================================================
// Example: an end-to-end Optimizely/Episerver CMS 11 content slice
// -----------------------------------------------------------------------------
// Shows how the pieces connect: content type -> controller -> view model -> service,
// plus a content event handler in an init module. EPiServer.* 11.x on .NET Framework 4.x.
// Genericized (Acme / StandardPage). Illustrative single file; in a real solution these
// live in separate folders (Models/, Controllers/, Business/Initialization/).
//
// Naming: "Optimizely CMS 11" == "Episerver CMS 11"; code/namespaces are EPiServer.* at v11.
// Platform layer (csproj/web.config/OWIN/binding redirects) -> developing-with-dotnet-4x.
// =============================================================================
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Web.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Acme.Web.Example
{
    // ---- Content type (page) -------------------------------------------------
    [ContentType(DisplayName = "Standard Page", GUID = "0c19c5e6-0000-0000-0000-0000000000a1",
        Description = "A general content page.", GroupName = "Acme")]
    public class StandardPage : PageData
    {
        [Display(Name = "Heading", Order = 10), CultureSpecific, Required]
        public virtual string Heading { get; set; }    // MUST be virtual to persist

        [Display(Name = "Main body", Order = 20), CultureSpecific]
        public virtual XhtmlString MainBody { get; set; }

        [Display(Name = "Main content area", Order = 30)]
        public virtual ContentArea MainContentArea { get; set; }
    }

    // ---- View model ----------------------------------------------------------
    public class StandardPageViewModel
    {
        public StandardPage CurrentPage { get; set; }
        public IEnumerable<PageData> Children { get; set; }
    }

    // ---- Service (read access via IContentLoader) ----------------------------
    public interface IPageQueryService
    {
        StandardPage Get(ContentReference link);
        IEnumerable<PageData> Children(ContentReference parent);
        ContentReference Publish(StandardPage edited);
    }

    public class PageQueryService : IPageQueryService
    {
        private readonly IContentLoader _loader;
        private readonly IContentRepository _repository;

        public PageQueryService(IContentLoader loader, IContentRepository repository)
        {
            _loader = loader;
            _repository = repository;
        }

        public StandardPage Get(ContentReference link) => _loader.Get<StandardPage>(link);

        public IEnumerable<PageData> Children(ContentReference parent) =>
            _loader.GetChildren<PageData>(parent);

        public ContentReference Publish(StandardPage edited)
        {
            // NEVER mutate a loader result directly — clone first (it's shared from cache).
            var writable = (StandardPage)edited.CreateWritableClone();
            return _repository.Save(writable,
                EPiServer.DataAccess.SaveAction.Publish,
                EPiServer.Security.AccessLevel.NoAccess);
        }
    }

    // ---- Controller (PageController<T> binds the routed page) -----------------
    public class StandardPageController : PageController<StandardPage>
    {
        private readonly IPageQueryService _pages;
        public StandardPageController(IPageQueryService pages) => _pages = pages;

        public ActionResult Index(StandardPage currentPage)
        {
            var model = new StandardPageViewModel
            {
                CurrentPage = currentPage,
                Children = _pages.Children(currentPage.ContentLink).ToList()
            };
            return View(model);   // view renders with Html.PropertyFor (see PageView template)
        }
    }

    // ---- Init module: DI registration + publish event ------------------------
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class ExampleInitialization : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IPageQueryService, PageQueryService>();
        }

        public void Initialize(InitializationEngine context)
        {
            var events = context.Locate.Advanced.GetInstance<IContentEvents>();
            events.PublishedContent += OnPublished;
        }

        public void Uninitialize(InitializationEngine context)
        {
            var events = context.Locate.Advanced.GetInstance<IContentEvents>();
            events.PublishedContent -= OnPublished;   // always unsubscribe
        }

        private void OnPublished(object sender, ContentEventArgs e)
        {
            if (e.Content is StandardPage page)
            {
                // e.g. enqueue a Find re-index, clear a custom cache, etc.
            }
        }
    }
}
