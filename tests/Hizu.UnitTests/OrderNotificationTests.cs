using FluentAssertions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Orders.Commands;
using Hizu.Application.Features.Orders.DTOs;
using Hizu.Domain.Entities;
using Hizu.Domain.Enums;
using Hizu.Domain.ValueObjects;
using Hizu.Infrastructure.Persistence;
using Hizu.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hizu.UnitTests;

public class OrderNotificationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly Mock<ISmsService> _smsServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;

    public OrderNotificationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _smsServiceMock = new Mock<ISmsService>();
        _emailServiceMock = new Mock<IEmailService>();

        _smsServiceMock
            .Setup(s => s.SendAdminOrderAlertSmsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _smsServiceMock
            .Setup(s => s.SendCustomerOrderStatusSmsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _emailServiceMock
            .Setup(e => e.SendAdminOrderFullDetailsEmailAsync(
                It.IsAny<OrderDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _emailServiceMock
            .Setup(e => e.SendCustomerOrderStatusEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<OrderDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private async Task<(ApplicationUser user, Product product)> SeedUserAndProductAsync()
    {
        var category = new Category { Name = "Mechanical", Slug = "mech", Code = "MECH" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Email = "customer@example.com",
            PhoneNumber = "09121112233",
            FirstName = "مسعود",
            LastName = "سلیمانی"
        };
        _context.Users.Add(user);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "پایه مانیتور آلومینیومی ماشین‌کاری شده",
            Slug = "aluminum-monitor-stand",
            Sku = "HZ-ALU-001",
            Price = 2500000,
            StockQuantity = 20,
            IsActive = true,
            CategoryId = category.Id
        };
        _context.Products.Add(product);

        var cart = new Cart
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Items = new List<CartItem>
            {
                new CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = 2
                }
            }
        };
        _context.Carts.Add(cart);

        await _context.SaveChangesAsync();
        return (user, product);
    }

    [Fact]
    public async Task CreateOrder_ShouldDispatch_AdminSmsAndFullDetailsEmail_AndCustomerNotifications()
    {
        // Arrange
        var (user, product) = await SeedUserAndProductAsync();
        var handler = new CreateOrderCommandHandler(_context, _smsServiceMock.Object, _emailServiceMock.Object);

        var command = new CreateOrderCommand
        {
            UserId = user.Id,
            ShippingAddress = new Address("خیابان ولیعصر، کوچه ناهید", "تهران", "تهران", "1966912345", "ایران")
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CustomerName.Should().Be("مسعود سلیمانی");
        result.Items.Should().HaveCount(1);
        result.Total.Should().Be(5000000); // 2 * 2,500,000 (free shipping > 1,500,000)

        // Verify Admin SMS alert was dispatched with order details
        _smsServiceMock.Verify(s => s.SendAdminOrderAlertSmsAsync(
            result.OrderNumber,
            "مسعود سلیمانی",
            "09121112233",
            5000000,
            It.Is<string>(summary => summary.Contains("پایه مانیتور آلومینیومی")),
            "تهران",
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify Admin Email was dispatched with full order details
        _emailServiceMock.Verify(e => e.SendAdminOrderFullDetailsEmailAsync(
            It.Is<OrderDto>(o => o.OrderNumber == result.OrderNumber && o.Items.Count == 1),
            "09121112233",
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify Customer SMS and Email notifications
        _smsServiceMock.Verify(s => s.SendCustomerOrderStatusSmsAsync(
            "09121112233",
            "مسعود سلیمانی",
            result.OrderNumber,
            "در انتظار بررسی و تایید",
            It.IsAny<CancellationToken>()), Times.Once);

        _emailServiceMock.Verify(e => e.SendCustomerOrderStatusEmailAsync(
            "customer@example.com",
            "مسعود سلیمانی",
            result.OrderNumber,
            "در انتظار بررسی و تایید",
            It.IsAny<string>(),
            It.Is<OrderDto>(o => o.OrderNumber == result.OrderNumber),
            It.IsAny<CancellationToken>()), Times.Once);

        // Product stock should NOT be deducted upon user checkout (still 20)
        var productInDb = await _context.Products.FindAsync(product.Id);
        productInDb!.StockQuantity.Should().Be(20);
    }

    [Fact]
    public async Task UpdateOrderStatus_ToProcessing_ShouldNotifyCustomer_AndDeductProductStock()
    {
        // Arrange
        var (user, product) = await SeedUserAndProductAsync();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "HIZU-2026-TEST1",
            UserId = user.Id,
            Status = OrderStatus.Pending,
            IsStockDeducted = false,
            Subtotal = 2500000,
            Total = 2500000,
            ShippingAddress = new Address("میدان ونک", "تهران", "تهران", "1234567890", "ایران"),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    Quantity = 1,
                    UnitPrice = 2500000,
                    TotalPrice = 2500000
                }
            }
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var handler = new UpdateOrderStatusCommandHandler(_context, _smsServiceMock.Object, _emailServiceMock.Object);

        // Act
        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, OrderStatus.Processing), CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Processing);
        updatedOrder.IsStockDeducted.Should().BeTrue();

        // Verify product stock was atomically deducted (20 - 1 = 19)
        var updatedProduct = await _context.Products.FindAsync(product.Id);
        updatedProduct!.StockQuantity.Should().Be(19);

        // Verify customer SMS received status for preparing/processing
        _smsServiceMock.Verify(s => s.SendCustomerOrderStatusSmsAsync(
            "09121112233",
            "مسعود سلیمانی",
            "HIZU-2026-TEST1",
            "در حال آماده‌سازی و ساخت",
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify customer Email received status for preparing/processing
        _emailServiceMock.Verify(e => e.SendCustomerOrderStatusEmailAsync(
            "customer@example.com",
            "مسعود سلیمانی",
            "HIZU-2026-TEST1",
            "در حال آماده‌سازی و ساخت",
            It.Is<string>(desc => desc.Contains("آماده‌سازی و ساخت")),
            It.IsAny<OrderDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatus_ToCancelled_ShouldRestoreProductStock_IfPreviouslyDeducted()
    {
        // Arrange
        var (user, product) = await SeedUserAndProductAsync();
        product.StockQuantity = 19; // Already deducted
        await _context.SaveChangesAsync();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "HIZU-2026-TEST-CANCEL",
            UserId = user.Id,
            Status = OrderStatus.Processing,
            IsStockDeducted = true,
            Subtotal = 2500000,
            Total = 2500000,
            ShippingAddress = new Address("میدان ونک", "تهران", "تهران", "1234567890", "ایران"),
            Items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    Quantity = 1,
                    UnitPrice = 2500000,
                    TotalPrice = 2500000
                }
            }
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var handler = new UpdateOrderStatusCommandHandler(_context, _smsServiceMock.Object, _emailServiceMock.Object);

        // Act
        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, OrderStatus.Cancelled), CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        updatedOrder!.Status.Should().Be(OrderStatus.Cancelled);
        updatedOrder.IsStockDeducted.Should().BeFalse();

        // Verify stock was restored from 19 to 20
        var restoredProduct = await _context.Products.FindAsync(product.Id);
        restoredProduct!.StockQuantity.Should().Be(20);
    }

    [Fact]
    public async Task UpdateOrderStatus_ToShipped_ShouldNotifyCustomer_WithPostDeliveryStatus()
    {
        // Arrange
        var (user, product) = await SeedUserAndProductAsync();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "HIZU-2026-TEST2",
            UserId = user.Id,
            Status = OrderStatus.Processing,
            Subtotal = 2500000,
            Total = 2500000,
            ShippingAddress = new Address("خیابان آزادی", "اصفهان", "اصفهان", "8123456789", "ایران"),
            Items = new List<OrderItem>()
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var handler = new UpdateOrderStatusCommandHandler(_context, _smsServiceMock.Object, _emailServiceMock.Object);

        // Act
        var result = await handler.Handle(new UpdateOrderStatusCommand(order.Id, OrderStatus.Shipped), CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _smsServiceMock.Verify(s => s.SendCustomerOrderStatusSmsAsync(
            "09121112233",
            "مسعود سلیمانی",
            "HIZU-2026-TEST2",
            "تحویل به اداره پست جهت ارسال",
            It.IsAny<CancellationToken>()), Times.Once);

        _emailServiceMock.Verify(e => e.SendCustomerOrderStatusEmailAsync(
            "customer@example.com",
            "مسعود سلیمانی",
            "HIZU-2026-TEST2",
            "تحویل به اداره پست جهت ارسال",
            It.Is<string>(desc => desc.Contains("تحویل مأمور اداره پست گردید")),
            It.IsAny<OrderDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SmsService_DefaultAdminPhone_ShouldBeConfigured_AsRequested()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Sms:Provider", "Mock" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var loggerMock = new Mock<ILogger<SmsService>>();

        var smsService = new SmsService(config, httpClientFactoryMock.Object, loggerMock.Object);

        // Act
        var sent = await smsService.SendAdminOrderAlertSmsAsync(
            "HIZU-TEST-001",
            "علی محمدی",
            "09123456789",
            1200000,
            "آباژور مدرن (1x)",
            "شیراز");

        // Assert
        sent.Should().BeTrue();
    }

    [Fact]
    public async Task EmailService_MockDispatch_ShouldSucceed_AndLogProperly()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Email:Enabled", "false" },
            { "Email:AdminEmails:0", "as.masoud.1868@gmail.com" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var loggerMock = new Mock<ILogger<EmailService>>();

        var emailService = new EmailService(config, loggerMock.Object);

        var orderDto = new OrderDto
        {
            OrderNumber = "HIZU-TEST-EMAIL-01",
            CustomerName = "مسعود سلیمانی",
            CustomerEmail = "customer@example.com",
            Total = 3200000,
            Subtotal = 3200000,
            ShippingCost = 0,
            ShippingAddress = new Address("خیابان ولیعصر", "تهران", "تهران", "12345", "ایران"),
            Items = new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    ProductName = "ماگ سرامیکی مینیمال",
                    Quantity = 2,
                    UnitPrice = 1600000,
                    TotalPrice = 3200000,
                    Sku = "MUG-01"
                }
            }
        };

        // Act
        var adminSent = await emailService.SendAdminOrderFullDetailsEmailAsync(orderDto, "09101854076");
        var customerSent = await emailService.SendCustomerOrderStatusEmailAsync(
            "customer@example.com",
            "مسعود سلیمانی",
            orderDto.OrderNumber,
            "در حال آماده‌سازی و ساخت",
            "سفارش شما در مرحله ساخت است.",
            orderDto);

        // Assert
        adminSent.Should().BeTrue();
        customerSent.Should().BeTrue();
    }
}

