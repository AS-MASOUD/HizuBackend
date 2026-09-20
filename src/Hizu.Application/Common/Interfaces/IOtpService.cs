namespace Hizu.Application.Common.Interfaces;

public record OtpSendResult(bool Success, string Message, int CooldownSeconds, string? DebugCode = null);

public interface IOtpService
{
    Task<OtpSendResult> SendOtpAsync(string recipient, string purpose = "login", CancellationToken cancellationToken = default);
    Task<bool> VerifyOtpAsync(string recipient, string code, string purpose = "login", CancellationToken cancellationToken = default);
}
