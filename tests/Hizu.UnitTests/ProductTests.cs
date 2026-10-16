using FluentAssertions;
using Hizu.Application.Features.Products.Commands;
using Hizu.Domain.Entities;
using Hizu.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hizu.UnitTests;

public class ProductTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public ProductTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateProduct_And_UpdateProductWithImages_ShouldSucceedWithoutConcurrencyException()
    {
        // Arrange
        var category = new Category
        {
            Name = "3D Print",
            Slug = "3d-print",
            Code = "3DP",
            Description = "3D Print Description"
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var createHandler = new CreateProductCommandHandler(_context);
        var createCommand = new CreateProductCommand
        {
            Name = "Test Robotic Part",
            Sku = "TRP-01",
            Price = 2500000,
            StockQuantity = 5,
            CategoryId = category.Id,
            ImageUrls = ["https://example.com/img1.jpg", "https://example.com/img2.jpg"]
        };

        // Act - Create
        var created = await createHandler.Handle(createCommand, CancellationToken.None);

        // Assert - Create
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();

        var savedProduct = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == created.Id);
        savedProduct.Should().NotBeNull();
        savedProduct!.Images.Should().HaveCount(2);

        // Act - Update images
        var updateHandler = new UpdateProductCommandHandler(_context);
        var updateCommand = new UpdateProductCommand
        {
            Id = created.Id,
            Name = "Updated Robotic Part",
            Sku = "TRP-01",
            Price = 3000000,
            StockQuantity = 8,
            CategoryId = category.Id,
            ImageUrls = ["https://example.com/img3.jpg"]
        };

        var updated = await updateHandler.Handle(updateCommand, CancellationToken.None);

        // Assert - Update
        updated.Should().NotBeNull();
        var refreshedProduct = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == created.Id);
        refreshedProduct!.Images.Should().HaveCount(1);
        refreshedProduct.Images.First().ImageUrl.Should().Be("https://example.com/img3.jpg");
    }

    [Fact]
    public async Task CreateProduct_WithoutSku_ShouldGenerateAutomatedSkuAndSlug()
    {
        // Arrange
        var category = new Category
        {
            Name = "CNC Milling",
            Slug = "cnc-milling",
            Code = "CNC",
            Description = "CNC Description"
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var createHandler = new CreateProductCommandHandler(_context);
        var createCommand = new CreateProductCommand
        {
            Name = "Precision Aluminum Gear",
            Sku = null, // Left empty intentionally to trigger automatic coding
            Price = 1200000,
            StockQuantity = 15,
            CategoryId = category.Id,
        };

        // Act
        var created = await createHandler.Handle(createCommand, CancellationToken.None);

        // Assert
        created.Should().NotBeNull();
        created.Sku.Should().StartWith("HZ-CNC-");
        created.Slug.Should().NotBeNullOrWhiteSpace();
        created.Slug.Should().Contain("precision-aluminum-gear");
    }

    [Fact]
    public async Task ProductComment_CreateAndReply_ShouldWorkProperly()
    {
        // Arrange product
        var category = new Category { Name = "Test Cat", Slug = "test-cat", Code = "TC" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Industrial Pulley",
            Slug = "industrial-pulley",
            Sku = "HZ-TC-0001",
            Price = 500000,
            StockQuantity = 10,
            CategoryId = category.Id
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Act - Submit Comment
        var commentHandler = new CreateProductCommentCommandHandler(_context);
        var comment = await commentHandler.Handle(new CreateProductCommentCommand
        {
            ProductId = product.Id,
            AuthorName = "Eng. Tehrani",
            AuthorEmail = "tehrani@example.com",
            Content = "عالی و بسیار باکیفیت ساخته شده است.",
            Rating = 5
        }, CancellationToken.None);

        // Assert - Not approved initially
        comment.Should().NotBeNull();
        comment.IsApproved.Should().BeFalse();
        comment.AuthorName.Should().Be("Eng. Tehrani");

        // Act - Admin Reply
        var replyHandler = new ReplyProductCommentCommandHandler(_context);
        var replied = await replyHandler.Handle(new ReplyProductCommentCommand(
            comment.Id,
            "با تشکر از اعتماد شما به کارگاه هیزو."
        ), CancellationToken.None);

        // Assert - Auto-approved with reply
        replied.IsApproved.Should().BeTrue();
        replied.AdminReply.Should().Be("با تشکر از اعتماد شما به کارگاه هیزو.");
        replied.AdminRepliedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProduct_WithNullStockQuantity_ShouldPersistNullStock()
    {
        // Arrange
        var category = new Category
        {
            Name = "Laser Cut",
            Slug = "laser-cut",
            Code = "LC",
            Description = "Laser Description"
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var createHandler = new CreateProductCommandHandler(_context);
        var createCommand = new CreateProductCommand
        {
            Name = "Custom Acrylic Plate",
            Price = 450000,
            StockQuantity = null, // Admin didn't enter stock
            CategoryId = category.Id
        };

        // Act
        var created = await createHandler.Handle(createCommand, CancellationToken.None);

        // Assert
        created.Should().NotBeNull();
        created.StockQuantity.Should().BeNull();

        var inDb = await _context.Products.FindAsync(created.Id);
        inDb.Should().NotBeNull();
        inDb!.StockQuantity.Should().BeNull();
    }
}

