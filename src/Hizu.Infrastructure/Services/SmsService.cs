using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hizu.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hizu.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SmsService> _logger;

    public SmsService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SmsService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizePhoneNumber(phoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            _logger.LogWarning("Invalid phone number provided for SMS: {Phone}", phoneNumber);
            return false;
        }

        var provider = _configuration["Sms:Provider"] ?? "Mock";
        var apiKey = _configuration["Sms:ApiKey"] ?? string.Empty;
        var senderNumber = _configuration["Sms:SenderNumber"] ?? "10008000";

        // If mock provider or no API key is specified, log formatted message for dev/staging testing
        if (provider.Equals("Mock", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogInformation("\n======================================================\n" +
                                   "[SMS MOCK DISPATCH - IRAN GATEWAY EMULATOR]\n" +
                                   "Provider: {Provider}\n" +
                                   "To:       {PhoneNumber}\n" +
                                   "Sender:   {Sender}\n" +
                                   "Message:\n{Message}\n" +
                                   "======================================================",
                                   provider, normalizedPhone, senderNumber, message);
            return true;
        }

        try
        {
            return provider.ToLowerInvariant() switch
            {
                "kavenegar" => await SendViaKavenegarAsync(normalizedPhone, senderNumber, apiKey, message, cancellationToken),
                "farazsms" or "ippanel" => await SendViaIpPanelAsync(normalizedPhone, senderNumber, apiKey, message, cancellationToken),
                "melipayamak" => await SendViaMelipayamakAsync(normalizedPhone, senderNumber, apiKey, message, cancellationToken),
                "ghasedak" => await SendViaGhasedakAsync(normalizedPhone, senderNumber, apiKey, message, cancellationToken),
                _ => await SendViaGenericHttpAsync(normalizedPhone, senderNumber, apiKey, message, cancellationToken)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispatch SMS to {Phone} using provider {Provider}", normalizedPhone, provider);
            return false;
        }
    }

    public async Task<bool> SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default)
    {
        var template = _configuration["Sms:OtpTemplate"] ?? "کد تایید ورود به هیزو: {Code}\nاعتبار: ۲ دقیقه";
        var message = template.Replace("{Code}", code);
        return await SendSmsAsync(phoneNumber, message, cancellationToken);
    }

    public async Task<bool> SendOrderNotificationAsync(string phoneNumber, string orderNumber, string status, CancellationToken cancellationToken = default)
    {
        var template = _configuration["Sms:OrderStatusTemplate"] ?? "وضعیت سفارش #{OrderNumber} شما به «{Status}» تغییر یافت.";
        var message = template
            .Replace("{OrderNumber}", orderNumber)
            .Replace("{Status}", status);
        return await SendSmsAsync(phoneNumber, message, cancellationToken);
    }

    public async Task<bool> SendCustomerOrderStatusSmsAsync(string phoneNumber, string customerName, string orderNumber, string statusTitle, CancellationToken cancellationToken = default)
    {
        var template = _configuration["Sms:CustomerStatusUpdateTemplate"] ??
                       "مشتری گرامی {CustomerName}،\nوضعیت سفارش #{OrderNumber} شما به «{Status}» تغییر یافت.\nاستودیو طراحی و ساخت هیزو";

        var message = template
            .Replace("{CustomerName}", string.IsNullOrWhiteSpace(customerName) ? "کاربر گرامی" : customerName)
            .Replace("{OrderNumber}", orderNumber)
            .Replace("{Status}", statusTitle);

        return await SendSmsAsync(phoneNumber, message, cancellationToken);
    }

    public async Task<bool> SendWelcomeAsync(string phoneNumber, string name, CancellationToken cancellationToken = default)
    {
        var template = _configuration["Sms:WelcomeTemplate"] ?? "{Name} گرامی، به استودیو طراحی و ساخت هیزو خوش آمدید.";
        var message = template.Replace("{Name}", string.IsNullOrWhiteSpace(name) ? "کاربر" : name);
        return await SendSmsAsync(phoneNumber, message, cancellationToken);
    }

    public async Task<bool> SendAdminNotificationAsync(string message, CancellationToken cancellationToken = default)
    {
        var adminPhones = _configuration.GetSection("Sms:AdminPhoneNumbers").Get<string[]>();
        if (adminPhones == null || adminPhones.Length == 0)
        {
            adminPhones = ["09101854076"];
        }

        var allSent = true;
        foreach (var phone in adminPhones)
        {
            var sent = await SendSmsAsync(phone, message, cancellationToken);
            if (!sent) allSent = false;
        }
        return allSent;
    }

    public async Task<bool> SendAdminOrderAlertSmsAsync(
        string orderNumber,
        string customerName,
        string customerPhone,
        decimal totalAmount,
        string itemsSummary,
        string city,
        CancellationToken cancellationToken = default)
    {
        var template = _configuration["Sms:AdminOrderAlertTemplate"] ??
                       "سفارش جدید در هیزو ثبت شد!\nشماره: #{OrderNumber}\nمشتری: {CustomerName} ({CustomerPhone})\nمبلغ کل: {Amount} تومان\nاقلام: {Items}\nشهر: {City}";

        var message = template
            .Replace("{OrderNumber}", orderNumber)
            .Replace("{CustomerName}", customerName)
            .Replace("{CustomerPhone}", customerPhone)
            .Replace("{Amount}", $"{totalAmount:N0}")
            .Replace("{Items}", itemsSummary)
            .Replace("{City}", city);

        return await SendAdminNotificationAsync(message, cancellationToken);
    }

    #region Provider Implementations for Iran SMS Gateways

    // 1. Kavenegar Gateway
    private async Task<bool> SendViaKavenegarAsync(string phone, string sender, string apiKey, string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("SmsClient");
        var url = _configuration["Sms:ApiUrl"] ?? "https://api.kavenegar.com/v1/{ApiKey}/sms/send.json";
        var requestUrl = url.Replace("{ApiKey}", Uri.EscapeDataString(apiKey));

        var formValues = new Dictionary<string, string>
        {
            { "receptor", phone },
            { "sender", sender },
            { "message", message }
        };

        var response = await client.PostAsync(requestUrl, new FormUrlEncodedContent(formValues), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Kavenegar API returned error HTTP {Code}: {Body}", response.StatusCode, errorBody);
            return false;
        }

        _logger.LogInformation("SMS dispatched via Kavenegar to {Phone}", phone);
        return true;
    }

    // 2. FarazSMS / IPPanel Gateway
    private async Task<bool> SendViaIpPanelAsync(string phone, string sender, string apiKey, string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("SmsClient");
        var url = _configuration["Sms:ApiUrl"] ?? "https://edge.ippanel.com/api/v1/sms/send/webservice/single";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("apikey", apiKey);

        var payload = new
        {
            recipient = new[] { phone },
            sender = sender,
            message = message
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("IPPanel/FarazSMS API returned error HTTP {Code}: {Body}", response.StatusCode, errorBody);
            return false;
        }

        _logger.LogInformation("SMS dispatched via IPPanel/FarazSMS to {Phone}", phone);
        return true;
    }

    // 3. Melipayamak Gateway
    private async Task<bool> SendViaMelipayamakAsync(string phone, string sender, string apiKey, string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("SmsClient");
        var url = _configuration["Sms:ApiUrl"] ?? "https://rest.payamak-panel.com/api/SendSMS/SendSMS";
        var userName = _configuration["Sms:UserName"] ?? string.Empty;

        var payload = new
        {
            username = userName,
            password = apiKey,
            to = phone,
            from = sender,
            text = message,
            isFlash = false
        };

        var response = await client.PostAsync(
            url,
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Melipayamak API returned error HTTP {Code}: {Body}", response.StatusCode, errorBody);
            return false;
        }

        _logger.LogInformation("SMS dispatched via Melipayamak to {Phone}", phone);
        return true;
    }

    // 4. Ghasedak Gateway
    private async Task<bool> SendViaGhasedakAsync(string phone, string sender, string apiKey, string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("SmsClient");
        var url = _configuration["Sms:ApiUrl"] ?? "https://api.ghasedak.me/v2/sms/send/simple";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("apikey", apiKey);

        var formValues = new Dictionary<string, string>
        {
            { "message", message },
            { "receptor", phone },
            { "linenumber", sender }
        };

        request.Content = new FormUrlEncodedContent(formValues);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Ghasedak API returned error HTTP {Code}: {Body}", response.StatusCode, errorBody);
            return false;
        }

        _logger.LogInformation("SMS dispatched via Ghasedak to {Phone}", phone);
        return true;
    }

    // 5. Generic HTTP Gateway
    private async Task<bool> SendViaGenericHttpAsync(string phone, string sender, string apiKey, string message, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("SmsClient");
        var url = _configuration["Sms:ApiUrl"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(url))
        {
            _logger.LogWarning("Generic SMS provider configured but Sms:ApiUrl is empty.");
            return false;
        }

        var requestUrl = url
            .Replace("{ApiKey}", Uri.EscapeDataString(apiKey))
            .Replace("{Receptor}", Uri.EscapeDataString(phone))
            .Replace("{Sender}", Uri.EscapeDataString(sender))
            .Replace("{Message}", Uri.EscapeDataString(message));

        HttpResponseMessage response;
        if (url.Contains("{Message}") || url.Contains("{Receptor}"))
        {
            response = await client.GetAsync(requestUrl, cancellationToken);
        }
        else
        {
            var formValues = new Dictionary<string, string>
            {
                { "receptor", phone },
                { "sender", sender },
                { "message", message }
            };
            response = await client.PostAsync(requestUrl, new FormUrlEncodedContent(formValues), cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Generic SMS Gateway returned error HTTP {Code}: {Body}", response.StatusCode, errorBody);
            return false;
        }

        _logger.LogInformation("SMS dispatched via Generic HTTP gateway to {Phone}", phone);
        return true;
    }

    #endregion

    private static string NormalizePhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        var cleaned = Regex.Replace(phone.Trim(), @"[^\d+]", "");
        if (cleaned.StartsWith("+98"))
        {
            cleaned = "0" + cleaned[3..];
        }
        else if (cleaned.StartsWith("0098"))
        {
            cleaned = "0" + cleaned[4..];
        }
        else if (cleaned.StartsWith("98"))
        {
            cleaned = "0" + cleaned[2..];
        }
        else if (!cleaned.StartsWith("0") && cleaned.Length == 10)
        {
            cleaned = "0" + cleaned;
        }
        return cleaned;
    }
}
