using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hizu.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hizu.Infrastructure.Services;

/// <summary>
/// Official implementation of Bale Safir OTP delivery API (https://docs.bale.ai/safir).
/// Sends OTP verification codes directly to Iranian phone numbers through Bale messenger.
/// </summary>
public class BaleOtpService : IBaleOtpService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BaleOtpService> _logger;

    public const string DefaultApiUrl = "https://safir.bale.ai/api/v3/send_message";

    public BaleOtpService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<BaleOtpService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<BaleOtpResult> SendOtpAsync(
        string phoneNumber,
        string otp,
        string? requestId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumberForBale(phoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            _logger.LogWarning("Bale OTP: Invalid Iranian phone number: '{Phone}'", phoneNumber);
            return new BaleOtpResult(false, ErrorCode: 8, ErrorMessage: "شماره تلفن وارد شده نامعتبر است.");
        }

        var isEnabled = _configuration.GetValue<bool>("Bale:Enabled", false);
        var apiAccessKey = _configuration["Bale:ApiAccessKey"] ?? string.Empty;
        var botId = _configuration.GetValue<long>("Bale:BotId", 0);
        var apiUrl = _configuration["Bale:ApiUrl"] ?? DefaultApiUrl;
        var provider = _configuration["Bale:Provider"] ?? string.Empty;

        var reqId = string.IsNullOrWhiteSpace(requestId)
            ? Guid.NewGuid().ToString("N")
            : requestId;

        // Mock emulator support for dev/testing when provider is Mock or key is not set
        if (provider.Equals("Mock", StringComparison.OrdinalIgnoreCase) ||
            (!isEnabled && _configuration["Bale:Mode"]?.Equals("Mock", StringComparison.OrdinalIgnoreCase) == true))
        {
            _logger.LogInformation("\n======================================================\n" +
                                   "[BALE SAFIR MOCK DISPATCH]\n" +
                                   "To:         {PhoneNumber}\n" +
                                   "Bot ID:     {BotId}\n" +
                                   "OTP Code:   {Otp}\n" +
                                   "Request ID: {RequestId}\n" +
                                   "======================================================",
                                   normalizedPhone, botId, otp, reqId);

            return new BaleOtpResult(
                Success: true,
                MessageId: $"mock-bale-{Guid.NewGuid():N}",
                ErrorCode: null,
                ErrorMessage: null,
                IsNotBaleUser: false);
        }

        if (!isEnabled || string.IsNullOrWhiteSpace(apiAccessKey) || botId <= 0)
        {
            _logger.LogWarning("Bale OTP is disabled or credentials (ApiAccessKey / BotId) are not configured.");
            return new BaleOtpResult(
                Success: false,
                ErrorCode: null,
                ErrorMessage: "سرویس پیام‌رسان بله فعال یا پیکربندی نشده است.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Headers.Add("api-access-key", apiAccessKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                request_id = reqId,
                bot_id = botId,
                phone_number = normalizedPhone,
                message_data = new
                {
                    otp_message = new
                    {
                        otp
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Dispatching Bale OTP to {Phone} (Request ID: {RequestId})", normalizedPhone, reqId);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                _logger.LogWarning("Bale Safir API returned empty response with HTTP status {StatusCode}", response.StatusCode);
                return new BaleOtpResult(false, ErrorCode: (int)response.StatusCode, ErrorMessage: "پاسخ خالی از سرور بله دریافت شد.");
            }

            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            // Check for Bale error_data
            if (root.TryGetProperty("error_data", out var errorDataProp) &&
                errorDataProp.ValueKind != JsonValueKind.Null)
            {
                int? errorCode = null;
                string? errorMessage = null;

                if (errorDataProp.ValueKind == JsonValueKind.Array && errorDataProp.GetArrayLength() > 0)
                {
                    var firstError = errorDataProp[0];
                    if (firstError.TryGetProperty("error_code", out var ecProp))
                    {
                        errorCode = ecProp.GetInt32();
                    }
                    if (firstError.TryGetProperty("error_message", out var emProp))
                    {
                        errorMessage = emProp.GetString();
                    }
                }
                else if (errorDataProp.ValueKind == JsonValueKind.Object)
                {
                    if (errorDataProp.TryGetProperty("error_code", out var ecProp))
                    {
                        errorCode = ecProp.GetInt32();
                    }
                    if (errorDataProp.TryGetProperty("error_message", out var emProp))
                    {
                        errorMessage = emProp.GetString();
                    }
                }

                // If error found
                if (errorCode.HasValue || !string.IsNullOrWhiteSpace(errorMessage))
                {
                    var isNotBaleUser = errorCode == 17 ||
                                        (errorMessage?.Contains("NotBaleUser", StringComparison.OrdinalIgnoreCase) == true);

                    var friendlyMessage = MapBaleErrorCode(errorCode, errorMessage);
                    _logger.LogWarning("Bale Safir rejected request for {Phone}: Code={Code}, Message='{Error}', Friendly='{Friendly}'",
                        normalizedPhone, errorCode, errorMessage, friendlyMessage);

                    return new BaleOtpResult(
                        Success: false,
                        ErrorCode: errorCode,
                        ErrorMessage: friendlyMessage,
                        IsNotBaleUser: isNotBaleUser);
                }
            }

            // Check HTTP status code if error_data wasn't explicitly populated
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Bale Safir API HTTP error {StatusCode}: {ResponseBody}", response.StatusCode, responseBody);
                return new BaleOtpResult(
                    Success: false,
                    ErrorCode: (int)response.StatusCode,
                    ErrorMessage: $"خطای ارتباط با سرور بله (HTTP {response.StatusCode})");
            }

            // Extract message_id
            if (root.TryGetProperty("message_id", out var messageIdProp) &&
                !string.IsNullOrWhiteSpace(messageIdProp.GetString()))
            {
                var messageId = messageIdProp.GetString()!;
                _logger.LogInformation("Bale OTP successfully dispatched to {Phone} (Message ID: {MessageId})", normalizedPhone, messageId);

                return new BaleOtpResult(
                    Success: true,
                    MessageId: messageId,
                    ErrorCode: null,
                    ErrorMessage: null,
                    IsNotBaleUser: false);
            }

            _logger.LogWarning("Bale Safir API response did not contain message_id: {ResponseBody}", responseBody);
            return new BaleOtpResult(false, ErrorMessage: "شناسه پیام از بله دریافت نشد.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Bale OTP request was cancelled for {Phone}", normalizedPhone);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception while sending Bale OTP to {Phone}", normalizedPhone);
            return new BaleOtpResult(false, ErrorMessage: "خطای سیستمی در برقراری ارتباط با پیام‌رسان بله.");
        }
    }

    /// <summary>
    /// Normalizes any Iranian mobile number input (e.g. 09121234567, +989121234567, 989121234567, 9121234567)
    /// to Bale's required format: "989xxxxxxxxx" without spaces or '+' symbol.
    /// </summary>
    public static string NormalizePhoneNumberForBale(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;

        // Convert Persian/Arabic digits to Latin
        var sb = new StringBuilder(phone.Length);
        foreach (var ch in phone)
        {
            if (ch >= '۰' && ch <= '۹') sb.Append((char)('0' + (ch - '۰')));
            else if (ch >= '٠' && ch <= '٩') sb.Append((char)('0' + (ch - '٠')));
            else sb.Append(ch);
        }

        var cleaned = Regex.Replace(sb.ToString().Trim(), @"[^\d]", "");
        if (string.IsNullOrWhiteSpace(cleaned)) return string.Empty;

        if (cleaned.StartsWith("0098"))
        {
            cleaned = cleaned[2..]; // 00989... -> 989...
        }
        else if (cleaned.StartsWith("09"))
        {
            cleaned = "98" + cleaned[1..]; // 0912... -> 98912...
        }
        else if (cleaned.StartsWith("9") && cleaned.Length == 10)
        {
            cleaned = "98" + cleaned; // 912... -> 98912...
        }

        // Validate final Iranian mobile number format: 989 + 9 digits (total 12 digits)
        if (cleaned.StartsWith("989") && cleaned.Length == 12)
        {
            return cleaned;
        }

        return cleaned;
    }

    private static string MapBaleErrorCode(int? errorCode, string? rawMessage)
    {
        return errorCode switch
        {
            17 => "شماره همراه کاربر در پیام‌رسان بله عضو نیست.",
            3 => "محدودیت تعداد درخواست ارسال کد پیام‌رسان بله فراتر رفته است.",
            8 => "فرمت شماره تلفن در پیام‌رسان بله نامعتبر است.",
            20 => "اعتبار پنل بله ناکافی است.",
            2 => "خطای داخلی سرور بله رخ داده است.",
            _ => rawMessage ?? "خطا در ارسال پیام از طریق بله."
        };
    }
}
