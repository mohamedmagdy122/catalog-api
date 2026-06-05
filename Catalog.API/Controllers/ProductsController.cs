using System.Diagnostics;
using Catalog.API.Data;
using Catalog.API.Metrics;
using Catalog.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/catalog")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetAll([FromQuery] int? categoryId)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();
        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var products = await query.OrderBy(p => p.Id).ToListAsync();
        return Ok(products);
    }

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        AppMetrics.ProductViewsTotal.WithLabels(product.Category?.Name ?? "unknown").Inc();
        return Ok(product);
    }

    [HttpGet("products/search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query parameter 'q' is required." });

        var sw = Stopwatch.StartNew();
        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.Name.Contains(q) || (p.Description != null && p.Description.Contains(q)))
            .OrderBy(p => p.Name)
            .ToListAsync();
        sw.Stop();
        AppMetrics.SearchDuration.Observe(sw.Elapsed.TotalSeconds);

        return Ok(products);
    }

    [Authorize]
    [HttpPost("products")]
    public async Task<IActionResult> Create([FromBody] ProductCreateRequest req)
    {
        var category = await _db.Categories.FindAsync(req.CategoryId);
        if (category == null)
            return BadRequest(new { message = "Invalid category." });

        var product = new Product
        {
            Name = req.Name,
            Description = req.Description,
            Price = req.Price,
            Stock = req.Stock,
            CategoryId = req.CategoryId,
            ImageUrl = req.ImageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        AppMetrics.ProductsTotal.Inc();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [Authorize]
    [HttpPut("products/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductUpdateRequest req)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        if (req.Name != null) product.Name = req.Name;
        if (req.Description != null) product.Description = req.Description;
        if (req.Price.HasValue) product.Price = req.Price.Value;
        if (req.Stock.HasValue) product.Stock = req.Stock.Value;
        if (req.CategoryId.HasValue)
        {
            var category = await _db.Categories.FindAsync(req.CategoryId.Value);
            if (category == null) return BadRequest(new { message = "Invalid category." });
            product.CategoryId = req.CategoryId.Value;
        }
        if (req.ImageUrl != null) product.ImageUrl = req.ImageUrl;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(product);
    }

    [Authorize]
    [HttpDelete("products/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        AppMetrics.ProductsTotal.Dec();

        return NoContent();
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _db.Categories.OrderBy(c => c.Id).ToListAsync();
        return Ok(categories);
    }
}
