using FluentAssertions;
using Hizu.Application.Common.Interfaces;
using Hizu.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hizu.UnitTests;

public class OtpServiceTests
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ISmsService> _smsServiceMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IBaleOtpService> _baleOtpServiceMock = new();
    private readonly Mock<ILogger<OtpService>> _loggerMock = new();

    public OtpServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    private IConfiguration CreateConfiguration(bool baleEnabled = true, bool preferBale = true, bool fallbackToSms = true)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Bale:Enabled"] = baleEnabled.ToString(),
            ["Bale:PreferBale"] = preferBale.ToString(),
            ["Bale:FallbackToSms"] = fallbackToSms.ToString()
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    [Fact]
    public async Task SendOtpAsync_WhenBaleEnabledAndSucceeds_DispatchesViaBaleWithoutSms()
    {
        // Arrange
        var config = CreateConfiguration(baleEnabled: true, preferBale: true);

        _baleOtpServiceMock
            .Setup(b => b.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaleOtpResult(Success: true, MessageId: "bale-msg-100"));

        var service = new OtpService(
            _memoryCache,
            _smsServiceMock.Object,
            _emailServiceMock.Object,
            _baleOtpServiceMock.Object,
            config,
            _loggerMock.Object);

        // Act
        var result = await service.SendOtpAsync("09121234567");

        // Assert
        result.Success.Should().BeTrue();
        result.DeliveryChannel.Should().Be("bale");
        result.Message.Should().Contain("بله");

        _baleOtpServiceMock.Verify(b => b.SendOtpAsync("09121234567", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _smsServiceMock.Verify(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendOtpAsync_WhenBaleUserNotFound_FallsBackToSms()
    {
        // Arrange
        var config = CreateConfiguration(baleEnabled: true, preferBale: true, fallbackToSms: true);

        _baleOtpServiceMock
            .Setup(b => b.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaleOtpResult(
                Success: false,
                ErrorCode: 17,
                ErrorMessage: "شماره همراه کاربر در پیام‌رسان بله عضو نیست.",
                IsNotBaleUser: true));

        _smsServiceMock
            .Setup(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new OtpService(
            _memoryCache,
            _smsServiceMock.Object,
            _emailServiceMock.Object,
            _baleOtpServiceMock.Object,
            config,
            _loggerMock.Object);

        // Act
        var result = await service.SendOtpAsync("09121234567");

        // Assert
        result.Success.Should().BeTrue();
        result.DeliveryChannel.Should().Be("sms");
        result.Message.Should().Contain("پیامک");

        _baleOtpServiceMock.Verify(b => b.SendOtpAsync("09121234567", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _smsServiceMock.Verify(s => s.SendOtpAsync("09121234567", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendOtpAsync_WhenBaleDisabled_SendsDirectlyViaSms()
    {
        // Arrange
        var config = CreateConfiguration(baleEnabled: false);

        _smsServiceMock
            .Setup(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new OtpService(
            _memoryCache,
            _smsServiceMock.Object,
            _emailServiceMock.Object,
            _baleOtpServiceMock.Object,
            config,
            _loggerMock.Object);

        // Act
        var result = await service.SendOtpAsync("09121234567");

        // Assert
        result.Success.Should().BeTrue();
        result.DeliveryChannel.Should().Be("sms");

        _baleOtpServiceMock.Verify(b => b.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _smsServiceMock.Verify(s => s.SendOtpAsync("09121234567", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendOtpAsync_WithinCooldown_RejectsResend()
    {
        // Arrange
        var config = CreateConfiguration(baleEnabled: false);

        _smsServiceMock
            .Setup(s => s.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new OtpService(
            _memoryCache,
            _smsServiceMock.Object,
            _emailServiceMock.Object,
            _baleOtpServiceMock.Object,
            config,
            _loggerMock.Object);

        // First send
        var firstResult = await service.SendOtpAsync("09121234567");
        firstResult.Success.Should().BeTrue();

        // Immediate second send
        var secondResult = await service.SendOtpAsync("09121234567");
        secondResult.Success.Should().BeFalse();
        secondResult.Message.Should().Contain("صبر نمایید");
    }

    [Fact]
    public async Task VerifyOtpAsync_ValidCode_ReturnsTrue()
    {
        // Arrange
        var config = CreateConfiguration(baleEnabled: true);
        string capturedCode = string.Empty;

        _baleOtpServiceMock
            .Setup(b => b.SendOtpAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string?, CancellationToken>((_, code, _, _) => capturedCode = code)
            .ReturnsAsync(new BaleOtpResult(Success: true, MessageId: "bale-msg-200"));

        var service = new OtpService(
            _memoryCache,
            _smsServiceMock.Object,
            _emailServiceMock.Object,
            _baleOtpServiceMock.Object,
            config,
            _loggerMock.Object);

        await service.SendOtpAsync("09121234567");
        capturedCode.Should().NotBeNullOrWhiteSpace();

        // Act
        var isValid = await service.VerifyOtpAsync("09121234567", capturedCode);

        // Assert
        isValid.Should().BeTrue();

        // Second verification should fail (one-time use)
        var isUsedAgain = await service.VerifyOtpAsync("09121234567", capturedCode);
        isUsedAgain.Should().BeFalse();
    }
}
