// Template: Optimizely Search & Navigation (Episerver Find) query service — EPiServer.Find 13.x on .NET Framework 4.x
// Packages: EPiServer.Find / Find.Cms / Find.Framework 13.x (net461)
// Placeholders: {{Namespace}} {{Page}}   ({{Page}} -> StandardPage)
//
// Inject IClient (or use SearchClient.Instance). For front-end queries, ALWAYS apply
// .FilterForVisitor() so access rights and publication status are honored.
using EPiServer.Find;
using EPiServer.Find.Cms;
using EPiServer.Find.Framework;
using System.Collections.Generic;

namespace {{Namespace}}.Search
{
    public class SearchService
    {
        private readonly IClient _findClient;

        public SearchService(IClient findClient)
        {
            _findClient = findClient;   // alternatively: SearchClient.Instance
        }

        public IEnumerable<{{Namespace}}.Models.Pages.{{Page}}> Search(string term, int page = 0, int pageSize = 10)
        {
            return _findClient.Search<{{Namespace}}.Models.Pages.{{Page}}>()
                .For(term)                                  // free-text
                .Filter(x => x.Heading.AnyWordBeginsWith(term))   // typed structured filter
                .FilterForVisitor()                         // access + publish-status (front-end safety)
                .Skip(page * pageSize)
                .Take(pageSize)
                .GetContentResult();                        // hydrate CMS IContent
        }

        public ITypeSearch<{{Namespace}}.Models.Pages.{{Page}}> BuildAdvanced(string term)
        {
            var filter = _findClient.BuildFilter<{{Namespace}}.Models.Pages.{{Page}}>()
                .And(x => x.Heading.AnyWordBeginsWith(term))
                .Or(x => x.MainBody.Match(term));

            return _findClient.Search<{{Namespace}}.Models.Pages.{{Page}}>()
                .Filter(filter)
                .FilterForVisitor();
        }
    }
}
