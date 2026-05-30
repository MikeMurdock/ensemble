// =============================================================================
// Example: end-to-end ASP.NET Web API 2 + EF6 slice on .NET Framework 4.6.1
// -----------------------------------------------------------------------------
// A single annotated file showing how the layers connect in a legacy app:
//   DTO  <->  Controller (System.Web.Http)  ->  Service  ->  Repository  ->  EF6 DbContext
// Genericized domain (Acme / Product). Illustrative: in a real solution these
// types live in separate projects (Acme.Api / Acme.Core / Acme.Data).
//
// Package baseline (packages.config, net461):
//   Microsoft.AspNet.WebApi 5.2.3, EntityFramework 6.2.0, Newtonsoft.Json 13.0.3
// =============================================================================
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;

namespace Acme.Api.Example
{
    // ---- DTO (the API contract; decoupled from the EF entity) ----------------
    public class ProductDto
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(200)]
        public string Name { get; set; }
        [System.ComponentModel.DataAnnotations.Range(0, 1_000_000)]
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
    }

    // ---- EF6 entity + context ------------------------------------------------
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
    }

    public class AcmeContext : DbContext
    {
        public AcmeContext() : base("name=DefaultConnection") { }
        public DbSet<Product> Products { get; set; }
    }

    // ---- Repository seam (keeps EF6 mockable + migration-friendly) -----------
    public interface IProductRepository
    {
        Product Get(int id);
        IReadOnlyList<Product> All();
        int Insert(Product entity);
        void Delete(int id);
    }

    public class ProductRepository : IProductRepository, IDisposable
    {
        private readonly AcmeContext _db;
        public ProductRepository(AcmeContext db) => _db = db;

        // AsNoTracking on read paths: skip change tracking for read-only queries.
        public Product Get(int id) => _db.Products.AsNoTracking().FirstOrDefault(p => p.Id == id);
        public IReadOnlyList<Product> All() => _db.Products.AsNoTracking().OrderBy(p => p.Name).ToList();

        public int Insert(Product entity)
        {
            _db.Products.Add(entity);
            _db.SaveChanges();           // EF6 assigns the identity key after save
            return entity.Id;
        }

        public void Delete(int id)
        {
            var entity = _db.Products.Find(id);
            if (entity == null) return;
            _db.Products.Remove(entity);
            _db.SaveChanges();
        }

        public void Dispose() => _db?.Dispose();
    }

    // ---- Service (domain rules; DTO <-> entity mapping) ----------------------
    public interface IProductService
    {
        ProductDto Find(int id);
        IReadOnlyList<ProductDto> All();
        ProductDto Add(ProductDto dto);
        void Remove(int id);
    }

    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        public ProductService(IProductRepository repo)
            => _repo = repo ?? throw new ArgumentNullException(nameof(repo));

        public ProductDto Find(int id)
        {
            if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
            var e = _repo.Get(id);
            return e == null ? null : ToDto(e);
        }

        public IReadOnlyList<ProductDto> All() => _repo.All().Select(ToDto).ToList();

        public ProductDto Add(ProductDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            var id = _repo.Insert(new Product { Name = dto.Name, Price = dto.Price, CategoryId = dto.CategoryId });
            dto.Id = id;
            return dto;
        }

        public void Remove(int id) => _repo.Delete(id);

        private static ProductDto ToDto(Product e) =>
            new ProductDto { Id = e.Id, Name = e.Name, Price = e.Price, CategoryId = e.CategoryId };
    }

    // ---- Web API 2 controller ------------------------------------------------
    [RoutePrefix("api/products")]
    public class ProductsController : ApiController
    {
        private readonly IProductService _service;
        public ProductsController(IProductService service)
            => _service = service ?? throw new ArgumentNullException(nameof(service));

        [HttpGet, Route("")]
        public IHttpActionResult List() => Ok(_service.All());

        [HttpGet, Route("{id:int}")]
        public IHttpActionResult Get(int id)
        {
            var dto = _service.Find(id);
            return dto == null ? (IHttpActionResult)NotFound() : Ok(dto);
        }

        [HttpPost, Route("")]
        public IHttpActionResult Create([FromBody] ProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var created = _service.Add(dto);
            return Created(new Uri(Request.RequestUri, $"/api/products/{created.Id}"), created);
        }

        [HttpDelete, Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            if (_service.Find(id) == null) return NotFound();
            _service.Remove(id);
            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }
    }
}
