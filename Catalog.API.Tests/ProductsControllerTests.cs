using Catalog.API.Controllers;
using Catalog.API.Data;
using Catalog.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Catalog.API.Tests;

public class ProductsControllerTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.Categories.Add(new Category { Id = 1, Name = "Electronics" });
        db.Products.AddRange(
            new Product { Id = 1, Name = "Wireless Mouse", Description = "Ergonomic", Price = 29.99m, Stock = 100, CategoryId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Product { Id = 2, Name = "Keyboard", Description = "Mechanical", Price = 79.99m, Stock = 50, CategoryId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task GetAll_ReturnsAllProducts()
    {
        var db = CreateDb();
        var controller = new ProductsController(db);

        var result = await controller.GetAll(null);

        var ok = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<IEnumerable<Product>>(ok.Value);
        Assert.Equal(2, products.Count());
    }

    [Fact]
    public async Task GetAll_FilterByCategory_ReturnsFiltered()
    {
        var db = CreateDb();
        db.Categories.Add(new Category { Id = 2, Name = "Books" });
        db.Products.Add(new Product { Id = 3, Name = "Novel", Price = 9.99m, Stock = 200, CategoryId = 2, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var controller = new ProductsController(db);
        var result = await controller.GetAll(2);

        var ok = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<IEnumerable<Product>>(ok.Value);
        Assert.Single(products);
    }

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsProduct()
    {
        var db = CreateDb();
        var controller = new ProductsController(db);

        var result = await controller.GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var product = Assert.IsType<Product>(ok.Value);
        Assert.Equal("Wireless Mouse", product.Name);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNotFound()
    {
        var db = CreateDb();
        var controller = new ProductsController(db);

        var result = await controller.GetById(999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Search_MatchingQuery_ReturnsResults()
    {
        var db = CreateDb();
        var controller = new ProductsController(db);

        var result = await controller.Search("Mouse");

        var ok = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<IEnumerable<Product>>(ok.Value);
        Assert.Single(products);
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsBadRequest()
    {
        var db = CreateDb();
        var controller = new ProductsController(db);

        var result = await controller.Search("");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetCategories_ReturnsAll()
    {
        var db = CreateDb();
        var controller = new ProductsController(db);

        var result = await controller.GetCategories();

        var ok = Assert.IsType<OkObjectResult>(result);
        var cats = Assert.IsAssignableFrom<IEnumerable<Category>>(ok.Value);
        Assert.Single(cats);
    }

    [Fact]
    public async Task Create_ValidProduct_ReturnsCreated()
    {
        var db = CreateDb();
        var controller = new ProductsController(db);

        var result = await controller.Create(new ProductCreateRequest
        {
            Name = "USB Hub",
            Description = "4-port USB 3.0",
            Price = 19.99m,
            Stock = 75,
            CategoryId = 1
        });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var product = Assert.IsType<Product>(created.Value);
        Assert.Equal("USB Hub", product.Name);
        Assert.Equal(3, await db.Products.CountAsync());
    }

    [Fact]
    public async Task Create_InvalidCategory_ReturnsBadRequest()
    {
        var db = CreateDb();
        var controller = new ProductsController(db);

        var result = await controller.Create(new ProductCreateRequest
        {
            Name = "Ghost",
            Price = 9.99m,
            Stock = 1,
            CategoryId = 999
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        var db = CreateDb();
        var controller = new ProductsController(db);

        var result = await controller.Delete(1);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(1, await db.Products.CountAsync());
    }

    [Fact]
    public async Task Delete_NonExisting_ReturnsNotFound()
    {
        var db = CreateDb();
        var controller = new ProductsController(db);

        var result = await controller.Delete(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
