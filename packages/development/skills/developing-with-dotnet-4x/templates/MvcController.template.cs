// Template: ASP.NET MVC 5 controller (System.Web.Mvc) — .NET Framework 4.x
// Package baseline: Microsoft.AspNet.Mvc 5.2.3 (net461)
// Placeholders: {{Namespace}} {{Entity}} {{entity}}
//   {{Namespace}} -> Acme.Web   {{Entity}} -> Product   {{entity}} -> product
using System;
using System.Web.Mvc;

namespace {{Namespace}}.Controllers
{
    public class {{Entity}}Controller : Controller
    {
        private readonly I{{Entity}}Service _service;

        public {{Entity}}Controller(I{{Entity}}Service service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // GET: /{{Entity}}
        public ActionResult Index()
        {
            return View(_service.All());
        }

        // GET: /{{Entity}}/Details/5
        public ActionResult Details(int id)
        {
            var {{entity}} = _service.Find(id);
            if ({{entity}} == null) return HttpNotFound();
            return View({{entity}});
        }

        // GET: /{{Entity}}/Create
        public ActionResult Create()
        {
            return View(new {{Entity}}ViewModel());
        }

        // POST: /{{Entity}}/Create
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Create({{Entity}}ViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var id = _service.Add(model);
            return RedirectToAction("Details", new { id });
        }

        // POST: /{{Entity}}/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            _service.Remove(id);
            return RedirectToAction("Index");
        }
    }
}
