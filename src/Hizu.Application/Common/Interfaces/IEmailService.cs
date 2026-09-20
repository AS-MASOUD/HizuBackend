using Hizu.Application.Features.Orders.DTOs;

namespace Hizu.Application.Common.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string bodyHtml, CancellationToken cancellationToken = default);
    Task<bool> SendOtpEmailAsync(string to, string code, CancellationToken cancellationToken = default);
    Task<bool> SendOrderEmailAsync(string to, string orderNumber, string detailsHtml, CancellationToken cancellationToken = default);
    Task<bool> SendAdminOrderAlertEmailAsync(string orderNumber, decimal totalAmount, string customerName, CancellationToken cancellationToken = default);
    Task<bool> SendAdminOrderFullDetailsEmailAsync(OrderDto order, string customerPhone, CancellationToken cancellationToken = default);
    Task<bool> SendCustomerOrderStatusEmailAsync(string to, string customerName, string orderNumber, string statusTitle, string statusDescription, OrderDto order, CancellationToken cancellationToken = default);
}
