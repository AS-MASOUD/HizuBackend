using System.Net;
using System.Text.Json;
using FluentAssertions;
using Hizu.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Hizu.UnitTests;

public class BaleOtpServiceTests
{
    private readonly Mock<ILogger<BaleOtpService>> _loggerMock = new();

    private IConfiguration CreateConfiguration(
        bool enabled = true,
        string apiKey = "test-bale-key",
        long botId = 123456789,
        string? provider = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Bale:Enabled"] = enabled.ToString(),
            ["Bale:ApiAccessKey"] = apiKey,
            ["Bale:BotId"] = botId.ToString(),
            ["Bale:ApiUrl"] = BaleOtpService.DefaultApiUrl,
            ["Bale:Provider"] = provider
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }

    [Fact]
    public async Task SendOtpAsync_WhenSuccessful_ReturnsMessageIdAndSuccess()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        string? interceptedApiKey = null;
        string? interceptedBody = null;

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                if (req.Headers.TryGetValues("api-access-key", out var values))
                {
                    interceptedApiKey = values.FirstOrDefault();
                }
                if (req.Content != null)
                {
                    interceptedBody = await req.Content.ReadAsStringAsync(ct);
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { message_id = "bale-msg-999888" }),
                        System.Text.Encoding.UTF8,
                        "application/json")
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var config = CreateConfiguration();
        var service = new BaleOtpService(httpClient, config, _loggerMock.Object);

        // Act
        var result = await service.SendOtpAsync("09121234567", "54321");

        // Assert
        result.Success.Should().BeTrue();
        result.MessageId.Should().Be("bale-msg-999888");
        result.IsNotBaleUser.Should().BeFalse();

        interceptedApiKey.Should().Be("test-bale-key");

        interceptedBody.Should().NotBeNullOrWhiteSpace();
        using var jsonDoc = JsonDocument.Parse(interceptedBody!);
        jsonDoc.RootElement.GetProperty("phone_number").GetString().Should().Be("989121234567");
        jsonDoc.RootElement.GetProperty("bot_id").GetInt64().Should().Be(123456789);
        jsonDoc.RootElement.GetProperty("message_data")
            .GetProperty("otp_message")
            .GetProperty("otp").GetString().Should().Be("54321");
    }

    [Fact]
    public async Task SendOtpAsync_WhenUserNotOnBale_ReturnsIsNotBaleUserTrue()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var baleErrorResponse = new
        {
            error_data = new[]
            {
                new { error_code = 17, error_message = "NotBaleUser" }
            }
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    JsonSerializer.Serialize(baleErrorResponse),
                    System.Text.Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var config = CreateConfiguration();
        var service = new BaleOtpService(httpClient, config, _loggerMock.Object);

        // Act
        var result = await service.SendOtpAsync("09121234567", "54321");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(17);
        result.IsNotBaleUser.Should().BeTrue();
        result.ErrorMessage.Should().Contain("عضو نیست");
    }

    [Fact]
    public async Task SendOtpAsync_WhenRateLimitExceeded_ReturnsRateLimitError()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var baleErrorResponse = new
        {
            error_data = new[]
            {
                new { error_code = 3, error_message = "RateLimitExceeded" }
            }
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent(
                    JsonSerializer.Serialize(baleErrorResponse),
                    System.Text.Encoding.UTF8,
                    "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var config = CreateConfiguration();
        var service = new BaleOtpService(httpClient, config, _loggerMock.Object);

        // Act
        var result = await service.SendOtpAsync("09121234567", "54321");

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(3);
        result.IsNotBaleUser.Should().BeFalse();
        result.ErrorMessage.Should().Contain("محدودیت");
    }

    [Theory]
    [InlineData("09121234567", "989121234567")]
    [InlineData("+989121234567", "989121234567")]
    [InlineData("00989121234567", "989121234567")]
    [InlineData("9121234567", "989121234567")]
    [InlineData("۰۹۱۲۱۲۳۴۵۶۷", "989121234567")] // Persian digits
    [InlineData(" 0912-123-4567 ", "989121234567")]
    public void NormalizePhoneNumberForBale_FormatsCorrectly(string input, string expected)
    {
        var normalized = BaleOtpService.NormalizePhoneNumberForBale(input);
        normalized.Should().Be(expected);
    }

    [Fact]
    public async Task SendOtpAsync_WhenInMockMode_ReturnsSuccessWithMockId()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);
        var config = CreateConfiguration(enabled: true, provider: "Mock");
        var service = new BaleOtpService(httpClient, config, _loggerMock.Object);

        // Act
        var result = await service.SendOtpAsync("09121234567", "12345");

        // Assert
        result.Success.Should().BeTrue();
        result.MessageId.Should().StartWith("mock-bale-");
    }
}
