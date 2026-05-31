// Template: Optimizely/Episerver CMS 11 page controller + view model — EPiServer.* 11.x on .NET Framework 4.x
// Packages: EPiServer.CMS.Core / EPiServer.CMS.AspNet 11.x (net461), ASP.NET MVC 5 (see developing-with-dotnet-4x).
// Placeholders: {{Namespace}} {{Page}}   ({{Page}} -> StandardPage)
//
// PageController<T> binds the routed content to the `currentPage` parameter.
// Render editable properties with Html.PropertyFor in the view (see PageView.template.cshtml).
using EPiServer;
using EPiServer.Core;
using EPiServer.Web.Mvc;
using System.Web.Mvc;

namespace {{Namespace}}.Controllers
{
    public class {{Page}}Controller : PageController<{{Namespace}}.Models.Pages.{{Page}}>
    {
        private readonly IContentLoader _contentLoader;

        public {{Page}}Controller(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }

        public ActionResult Index({{Namespace}}.Models.Pages.{{Page}} currentPage)
        {
            var model = new {{Namespace}}.Models.ViewModels.{{Page}}ViewModel
            {
                CurrentPage = currentPage,
                Children = _contentLoader.GetChildren<PageData>(currentPage.ContentLink)
            };
            return View(model);
        }
    }
}

namespace {{Namespace}}.Models.ViewModels
{
    public class {{Page}}ViewModel
    {
        public {{Namespace}}.Models.Pages.{{Page}} CurrentPage { get; set; }
        public System.Collections.Generic.IEnumerable<EPiServer.Core.PageData> Children { get; set; }
    }
}
