using FluentAssertions;
using Hizu.Application.Common.Helpers;
using Hizu.Application.Features.Cart.Commands;
using Hizu.Application.Features.Products.Commands;
using Hizu.Domain.Entities;
using Xunit;

namespace Hizu.UnitTests;

public class DomainAndApplicationTests
{
    [Fact]
    public void SlugHelper_ShouldGenerateCorrectSlug_ForPersianAndEnglish()
    {
        // Arrange
        var persianTitle = "کیس کیبورد برنجی شمش ۵محوره";

        // Act
        var slug = SlugHelper.GenerateSlug(persianTitle);

        // Assert
        slug.Should().Be("کیس-کیبورد-برنجی-شمش-۵محوره");
    }

    [Fact]
    public void SlugHelper_ShouldCleanSpecialCharactersAndSpaces()
    {
        // Arrange
        var messyTitle = "  Hizu  Fabrication // 5-Axis CNC & 3D Print!  ";

        // Act
        var slug = SlugHelper.GenerateSlug(messyTitle);

        // Assert
        slug.Should().Be("hizu-fabrication-5-axis-cnc-3d-print");
    }

    [Fact]
    public void Product_InitialStock_ShouldBeValid()
    {
        // Arrange & Act
        var product = new Product
        {
            Name = "AL-7075 Gripper",
            Sku = "HZ-GRP-7075",
            Price = 4500000,
            StockQuantity = 10,
            Tolerance = "±0.02 mm"
        };

        // Assert
        product.StockQuantity.Should().Be(10);
        product.Price.Should().Be(4500000);
        product.Tolerance.Should().Be("±0.02 mm");
    }

    [Fact]
    public void AddToCartCommandValidator_ShouldFail_WhenQuantityIsZeroOrNegative()
    {
        // Arrange
        var validator = new AddToCartCommandValidator();
        var command = new AddToCartCommand(Guid.NewGuid(), Guid.NewGuid(), 0);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddToCartCommand.Quantity));
    }

    [Fact]
    public void CreateProductCommandValidator_ShouldFail_WhenNameIsEmpty()
    {
        // Arrange
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand
        {
            Name = "",
            Sku = "TEST",
            Price = 1000,
            StockQuantity = 5,
            CategoryId = 1
        };

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.Name));
    }
}
