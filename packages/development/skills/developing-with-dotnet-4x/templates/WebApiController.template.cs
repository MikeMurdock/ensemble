// Template: ASP.NET Web API 2 controller (System.Web.Http) — .NET Framework 4.x
// Package baseline: Microsoft.AspNet.WebApi 5.2.3 (net461)
// Placeholders: {{Namespace}} {{Entity}} {{entity}} {{entities}}
//   {{Namespace}} -> Acme.Api      (root namespace)
//   {{Entity}}    -> Product       (PascalCase singular)
//   {{entity}}    -> product       (camel/lower singular)
//   {{entities}}  -> products      (lower plural, used in the route)
using System;
using System.Net;
using System.Web.Http;

namespace {{Namespace}}.Controllers
{
    /// <summary>CRUD surface for {{Entity}}. Returns IHttpActionResult for testable, status-correct responses.</summary>
    [RoutePrefix("api/{{entities}}")]
    public class {{Entity}}sController : ApiController
    {
        private readonly I{{Entity}}Service _service;

        // Constructor injection requires an IDependencyResolver wired in WebApiConfig.
        public {{Entity}}sController(I{{Entity}}Service service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet, Route("")]
        public IHttpActionResult List()
        {
            return Ok(_service.All());
        }

        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            var {{entity}} = _service.Find(id);
            return {{entity}} == null ? (IHttpActionResult)NotFound() : Ok({{entity}});
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] {{Entity}}Dto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = _service.Add(dto);
            var location = new Uri(Request.RequestUri, $"/api/{{entities}}/{created.Id}");
            return Created(location, created);
        }

        [HttpPut, Route("{id:int}")]
        public IHttpActionResult Update(int id, [FromBody] {{Entity}}Dto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (_service.Find(id) == null) return NotFound();

            _service.Update(id, dto);
            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            if (_service.Find(id) == null) return NotFound();

            _service.Remove(id);
            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}
