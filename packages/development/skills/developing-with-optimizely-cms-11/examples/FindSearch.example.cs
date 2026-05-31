// =============================================================================
// Example: Optimizely Search & Navigation (Episerver Find) query slice
// -----------------------------------------------------------------------------
// Shows a typed Find query feeding a search results view model, with the
// front-end safety filter applied. EPiServer.Find 13.x on .NET Framework 4.x.
// Genericized (Acme / StandardPage). Illustrative single file.
//
// Find vs legacy: this uses EPiServer.Find (the search engine). The older built-in
// EPiServer.Search (Lucene) is a different API and is not shown here.
// Platform layer -> developing-with-dotnet-4x.
// =============================================================================
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using EPiServer.Core;
using EPiServer.Find;
using EPiServer.Find.Cms;
using EPiServer.Find.Framework;

namespace Acme.Web.Example.Search
{
    // The content type being searched (see CmsContentSlice.example.cs).
    // Assume a StandardPage with Heading + MainBody indexed by Find conventions.

    public class SearchResultItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }

    public class SearchResultsViewModel
    {
        public string Term { get; set; }
        public int Total { get; set; }
        public IReadOnlyList<SearchResultItem> Items { get; set; }
    }

    public interface ISiteSearch
    {
        SearchResultsViewModel Search(string term, int page = 0, int pageSize = 10);
    }

    public class SiteSearch : ISiteSearch
    {
        private readonly IClient _find;
        private readonly EPiServer.Web.Routing.UrlResolver _urlResolver;

        public SiteSearch(IClient find, EPiServer.Web.Routing.UrlResolver urlResolver)
        {
            _find = find;             // or SearchClient.Instance
            _urlResolver = urlResolver;
        }

        public SearchResultsViewModel Search(string term, int page = 0, int pageSize = 10)
        {
            // Typed query: free-text + structured filter + visitor safety + paging.
            var results = _find.Search<Acme.Web.Example.StandardPage>()
                .For(term)
                .InField(x => x.Heading)
                .InField(x => x.MainBody)
                .FilterForVisitor()                       // honor access rights + publish status
                .Skip(page * pageSize)
                .Take(pageSize)
                .GetContentResult();                      // hydrate CMS IContent

            var items = results
                .Select(p => new SearchResultItem
                {
                    Title = p.Heading,
                    Url = _urlResolver.GetUrl(p.ContentLink)
                })
                .ToList();

            return new SearchResultsViewModel
            {
                Term = term,
                Total = results.TotalMatching,
                Items = items
            };
        }
    }

    // ---- Controller wiring the search to a view ------------------------------
    public class SearchController : Controller
    {
        private readonly ISiteSearch _search;
        public SearchController(ISiteSearch search) => _search = search;

        public ActionResult Index(string q, int page = 0)
        {
            var model = _search.Search(q ?? string.Empty, page);
            return View(model);
        }
    }
}
