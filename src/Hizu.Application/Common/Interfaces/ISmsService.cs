namespace Hizu.Application.Common.Interfaces;

public interface ISmsService
{
    Task<bool> SendSmsAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
    Task<bool> SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
    Task<bool> SendOrderNotificationAsync(string phoneNumber, string orderNumber, string status, CancellationToken cancellationToken = default);
    Task<bool> SendCustomerOrderStatusSmsAsync(string phoneNumber, string customerName, string orderNumber, string statusTitle, CancellationToken cancellationToken = default);
    Task<bool> SendWelcomeAsync(string phoneNumber, string name, CancellationToken cancellationToken = default);
    Task<bool> SendAdminNotificationAsync(string message, CancellationToken cancellationToken = default);
    Task<bool> SendAdminOrderAlertSmsAsync(string orderNumber, string customerName, string customerPhone, decimal totalAmount, string itemsSummary, string city, CancellationToken cancellationToken = default);
}
