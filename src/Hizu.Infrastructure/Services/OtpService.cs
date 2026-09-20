using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Hizu.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Hizu.Infrastructure.Services;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly ILogger<OtpService> _logger;

    private const int OtpExpiryMinutes = 2;
    private const int ResendCooldownSeconds = 60;
    private const int MaxVerifyAttempts = 5;

    public OtpService(
        IMemoryCache memoryCache,
        ISmsService smsService,
        IEmailService emailService,
        ILogger<OtpService> logger)
    {
        _memoryCache = memoryCache;
        _smsService = smsService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<OtpSendResult> SendOtpAsync(string recipient, string purpose = "login", CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRecipient(recipient);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return new OtpSendResult(false, "شماره تماس یا ایمیل نامعتبر است.", 0);
        }

        var cacheKey = GetCacheKey(normalized, purpose);

        // Check for cooldown
        if (_memoryCache.TryGetValue(cacheKey, out CachedOtp? existing) && existing != null)
        {
            var elapsedSeconds = (int)(DateTime.UtcNow - existing.CreatedAt).TotalSeconds;
            if (elapsedSeconds < ResendCooldownSeconds)
            {
                var remaining = ResendCooldownSeconds - elapsedSeconds;
                return new OtpSendResult(false, $"لطفاً {remaining} ثانیه دیگر جهت ارسال مجدد صبر نمایید.", remaining);
            }
        }

        // Generate 5-digit random numeric code
        var code = RandomNumberGenerator.GetInt32(10000, 99999).ToString();
        var entry = new CachedOtp
        {
            Code = code,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
            Purpose = purpose,
            Attempts = 0
        };

        _memoryCache.Set(cacheKey, entry, TimeSpan.FromMinutes(OtpExpiryMinutes));

        bool dispatched;
        if (normalized.Contains('@'))
        {
            dispatched = await _emailService.SendOtpEmailAsync(normalized, code, cancellationToken);
        }
        else
        {
            dispatched = await _smsService.SendOtpAsync(normalized, code, cancellationToken);
        }

        _logger.LogInformation("Generated OTP for {Recipient} (Purpose: {Purpose}) => [Code: {Code}]", normalized, purpose, code);

        return new OtpSendResult(
            Success: dispatched,
            Message: dispatched ? "کد تایید با موفقیت ارسال گردید." : "خطا در ارسال کد تایید.",
            CooldownSeconds: ResendCooldownSeconds,
            DebugCode: code
        );
    }

    public Task<bool> VerifyOtpAsync(string recipient, string code, string purpose = "login", CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRecipient(recipient);
        if (string.IsNullOrWhiteSpace(normalized) || string.IsNullOrWhiteSpace(code))
        {
            return Task.FromResult(false);
        }

        var cleanCode = code.Trim();
        var cacheKey = GetCacheKey(normalized, purpose);

        if (!_memoryCache.TryGetValue(cacheKey, out CachedOtp? cached) || cached == null)
        {
            _logger.LogWarning("No active OTP found for {Recipient} (Purpose: {Purpose})", normalized, purpose);
            return Task.FromResult(false);
        }

        if (DateTime.UtcNow > cached.ExpiresAt)
        {
            _memoryCache.Remove(cacheKey);
            _logger.LogWarning("OTP expired for {Recipient}", normalized);
            return Task.FromResult(false);
        }

        cached.Attempts++;
        if (cached.Attempts > MaxVerifyAttempts)
        {
            _memoryCache.Remove(cacheKey);
            _logger.LogWarning("Maximum OTP verification attempts reached for {Recipient}", normalized);
            return Task.FromResult(false);
        }

        if (cached.Code == cleanCode)
        {
            _memoryCache.Remove(cacheKey);
            _logger.LogInformation("OTP successfully verified for {Recipient}", normalized);
            return Task.FromResult(true);
        }

        _logger.LogWarning("Invalid OTP code submitted for {Recipient} (Attempt: {Attempt})", normalized, cached.Attempts);
        return Task.FromResult(false);
    }

    private static string GetCacheKey(string recipient, string purpose) => $"otp:{purpose}:{recipient.ToLowerInvariant()}";

    private static string NormalizeRecipient(string recipient)
    {
        if (string.IsNullOrWhiteSpace(recipient)) return string.Empty;
        var trimmed = recipient.Trim();

        if (trimmed.Contains('@'))
        {
            return trimmed.ToLowerInvariant();
        }

        var cleaned = Regex.Replace(trimmed, @"[^\d+]", "");
        if (cleaned.StartsWith("+98")) cleaned = "0" + cleaned[3..];
        else if (cleaned.StartsWith("0098")) cleaned = "0" + cleaned[4..];
        else if (cleaned.StartsWith("98")) cleaned = "0" + cleaned[2..];
        else if (!cleaned.StartsWith("0") && cleaned.Length == 10) cleaned = "0" + cleaned;

        return cleaned;
    }

    private class CachedOtp
    {
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public int Attempts { get; set; }
    }
}
