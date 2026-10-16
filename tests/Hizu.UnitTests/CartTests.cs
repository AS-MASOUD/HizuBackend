using FluentAssertions;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Features.Cart.Commands;
using Hizu.Application.Features.Cart.DTOs;
using Hizu.Application.Features.Cart.Queries;
using Hizu.Domain.Entities;
using Hizu.Infrastructure.Persistence;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Hizu.UnitTests;

public class CartTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly Mock<IMediator> _mediatorMock;

    public CartTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _mediatorMock = new Mock<IMediator>();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<(Category Category, Product Product, ApplicationUser User)> SeedBasicEntitiesAsync(int? stockQuantity = 10, bool isActive = true)
    {
        var category = new Category
        {
            Name = "CNC",
            Slug = "cnc",
            Code = "CNC",
            Description = "CNC Description"
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Test Product",
            Slug = "test-product",
            Sku = "TP-001",
            Price = 100000,
            StockQuantity = stockQuantity,
            IsActive = isActive,
            CategoryId = category.Id
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "test@example.com",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (category, product, user);
    }

    [Fact]
    public async Task AddToCart_ShouldSucceed_WhenCartDoesNotExist()
    {
        // Arrange
        var (_, product, user) = await SeedBasicEntitiesAsync();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CartDto { UserId = user.Id });

        var handler = new AddToCartCommandHandler(_context, _mediatorMock.Object);

        // Act
        var result = await handler.Handle(new AddToCartCommand(user.Id, product.Id, 2), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var savedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == user.Id);
        savedCart.Should().NotBeNull();
        savedCart!.Items.Should().HaveCount(1);
        var item = savedCart.Items.First();
        item.ProductId.Should().Be(product.Id);
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(100000);
        item.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task AddToCart_ShouldSucceed_WhenCartAlreadyExists()
    {
        // Arrange
        var (_, product, user) = await SeedBasicEntitiesAsync();

        var existingCart = new Cart
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Carts.Add(existingCart);
        await _context.SaveChangesAsync();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CartDto { UserId = user.Id });

        var handler = new AddToCartCommandHandler(_context, _mediatorMock.Object);

        // Act
        var result = await handler.Handle(new AddToCartCommand(user.Id, product.Id, 3), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var savedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == user.Id);
        savedCart.Should().NotBeNull();
        savedCart!.Items.Should().HaveCount(1);
        savedCart.Items.First().Quantity.Should().Be(3);
    }

    [Fact]
    public async Task AddToCart_ShouldAccumulateQuantity_WhenItemAlreadyInCart()
    {
        // Arrange
        var (_, product, user) = await SeedBasicEntitiesAsync(stockQuantity: 10);

        var existingCart = new Cart
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Carts.Add(existingCart);
        var existingItem = new CartItem
        {
            CartId = existingCart.Id,
            ProductId = product.Id,
            Quantity = 2,
            UnitPrice = product.Price
        };
        _context.CartItems.Add(existingItem);
        await _context.SaveChangesAsync();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CartDto { UserId = user.Id });

        var handler = new AddToCartCommandHandler(_context, _mediatorMock.Object);

        // Act
        await handler.Handle(new AddToCartCommand(user.Id, product.Id, 3), CancellationToken.None);

        // Assert
        var savedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == user.Id);
        savedCart!.Items.Should().HaveCount(1);
        savedCart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public async Task AddToCart_ShouldThrowBadRequest_WhenQuantityExceedsStock()
    {
        // Arrange
        var (_, product, user) = await SeedBasicEntitiesAsync(stockQuantity: 3);

        var handler = new AddToCartCommandHandler(_context, _mediatorMock.Object);

        // Act & Assert
        var act = async () => await handler.Handle(new AddToCartCommand(user.Id, product.Id, 5), CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*موجودی*");
    }

    [Fact]
    public async Task AddToCart_ShouldSucceed_WhenStockQuantityIsNull()
    {
        // Arrange
        var (_, product, user) = await SeedBasicEntitiesAsync(stockQuantity: null);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CartDto { UserId = user.Id });

        var handler = new AddToCartCommandHandler(_context, _mediatorMock.Object);

        // Act
        var result = await handler.Handle(new AddToCartCommand(user.Id, product.Id, 10), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var savedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == user.Id);
        savedCart.Should().NotBeNull();
        savedCart!.Items.First().Quantity.Should().Be(10);
    }

    [Fact]
    public async Task AddToCart_ShouldThrowNotFound_WhenProductIsInactive()
    {
        // Arrange
        var (_, product, user) = await SeedBasicEntitiesAsync(isActive: false);

        var handler = new AddToCartCommandHandler(_context, _mediatorMock.Object);

        // Act & Assert
        var act = async () => await handler.Handle(new AddToCartCommand(user.Id, product.Id, 1), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateCartItem_ShouldUpdateQuantity_WhenItemExists()
    {
        // Arrange
        var (_, product, user) = await SeedBasicEntitiesAsync(stockQuantity: 10);

        var existingCart = new Cart
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Carts.Add(existingCart);
        var existingItem = new CartItem
        {
            CartId = existingCart.Id,
            ProductId = product.Id,
            Quantity = 2,
            UnitPrice = product.Price
        };
        _context.CartItems.Add(existingItem);
        await _context.SaveChangesAsync();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CartDto { UserId = user.Id });

        var handler = new UpdateCartItemCommandHandler(_context, _mediatorMock.Object);

        // Act
        await handler.Handle(new UpdateCartItemCommand(user.Id, existingItem.Id, 4), CancellationToken.None);

        // Assert
        var updatedItem = await _context.CartItems.FindAsync(existingItem.Id);
        updatedItem.Should().NotBeNull();
        updatedItem!.Quantity.Should().Be(4);
    }

    [Fact]
    public async Task RemoveFromCart_ShouldRemoveItem()
    {
        // Arrange
        var (_, product, user) = await SeedBasicEntitiesAsync();

        var existingCart = new Cart
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Carts.Add(existingCart);
        var existingItem = new CartItem
        {
            CartId = existingCart.Id,
            ProductId = product.Id,
            Quantity = 2,
            UnitPrice = product.Price
        };
        _context.CartItems.Add(existingItem);
        await _context.SaveChangesAsync();

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCartQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CartDto { UserId = user.Id });

        var handler = new RemoveFromCartCommandHandler(_context, _mediatorMock.Object);

        // Act
        await handler.Handle(new RemoveFromCartCommand(user.Id, existingItem.Id), CancellationToken.None);

        // Assert
        var savedCart = await _context.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == user.Id);
        savedCart!.Items.Should().BeEmpty();
    }
}
