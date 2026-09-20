using System.Net;
using System.Net.Mail;
using System.Text;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Orders.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Hizu.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string bodyHtml, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to)) return false;

        var enabled = _configuration.GetValue<bool>("Email:Enabled", false);
        var host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = _configuration.GetValue<int>("Email:SmtpPort", 587);
        var enableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);
        var userName = _configuration["Email:UserName"] ?? string.Empty;
        var password = _configuration["Email:Password"] ?? string.Empty;
        var fromAddress = _configuration["Email:FromAddress"] ?? "noreply@hizu.ir";
        var fromName = _configuration["Email:FromName"] ?? "هیزو | استودیو طراحی و ساخت دیجیتال";

        if (!enabled || string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("\n======================================================\n" +
                                   "[EMAIL MOCK DISPATCH - SMTP EMULATOR]\n" +
                                   "To:      {To}\n" +
                                   "Subject: {Subject}\n" +
                                   "From:    {From} <{FromAddress}>\n" +
                                   "Body:\n{Body}\n" +
                                   "======================================================",
                                   to, subject, fromName, fromAddress, bodyHtml);
            return true;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(userName, password),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromName, Encoding.UTF8),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(to);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
            return false;
        }
    }

    public async Task<bool> SendOtpEmailAsync(string to, string code, CancellationToken cancellationToken = default)
    {
        var subject = "کد تایید ورود به هیزو";
        var body = $@"
<div dir='rtl' style='font-family: Tahoma, Arial, sans-serif; background-color: #F7F5EE; padding: 30px; border-radius: 16px; max-width: 500px; margin: auto; border: 1px solid #dfc79b;'>
    <div style='text-align: center; margin-bottom: 20px;'>
        <h2 style='color: #082719; margin: 0; font-size: 22px;'>هیزو // FABRICATION LAB</h2>
        <p style='color: #5A626A; font-size: 13px; margin-top: 5px;'>استودیو طراحی و ساخت دیجیتال</p>
    </div>
    <div style='background: #ffffff; padding: 25px; border-radius: 12px; text-align: center; box-shadow: 0 4px 12px rgba(0,0,0,0.05);'>
        <p style='font-size: 14px; color: #161A1D; margin-bottom: 15px;'>کد تایید یکبار مصرف شما برای ورود / ثبت‌نام:</p>
        <div style='font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #082719; background: #EDE8DC; padding: 12px; border-radius: 8px; display: inline-block; font-family: monospace;'>
            {code}
        </div>
        <p style='font-size: 12px; color: #737373; margin-top: 15px;'>این کد به مدت ۲ دقیقه معتبر است. در صورت عدم درخواست، این پیام را نادیده بگیرید.</p>
    </div>
</div>";

        return await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task<bool> SendOrderEmailAsync(string to, string orderNumber, string detailsHtml, CancellationToken cancellationToken = default)
    {
        var subject = $"تاییدیه ثبت سفارش #{orderNumber} در استودیو هیزو";
        var body = $@"
<div dir='rtl' style='font-family: Tahoma, Arial, sans-serif; background-color: #F7F5EE; padding: 30px; border-radius: 16px; max-width: 600px; margin: auto; border: 1px solid #dfc79b;'>
    <div style='text-align: center; margin-bottom: 20px;'>
        <h2 style='color: #082719; margin: 0;'>استودیو طراحی و ساخت هیزو</h2>
        <p style='color: #5A626A; font-size: 13px;'>سفارش شماره #{orderNumber}</p>
    </div>
    <div style='background: #ffffff; padding: 25px; border-radius: 12px; box-shadow: 0 4px 12px rgba(0,0,0,0.05);'>
        <p style='font-size: 14px; color: #161A1D; font-weight: bold;'>مشتری گرامی، سفارش شما با موفقیت در سیستم ثبت گردید.</p>
        <div style='margin-top: 15px; font-size: 13px; color: #333;'>
            {detailsHtml}
        </div>
    </div>
</div>";

        return await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task<bool> SendAdminOrderAlertEmailAsync(string orderNumber, decimal totalAmount, string customerName, CancellationToken cancellationToken = default)
    {
        var adminEmails = _configuration.GetSection("Email:AdminEmails").Get<string[]>();
        if (adminEmails == null || adminEmails.Length == 0)
        {
            adminEmails = ["as.masoud.1868@gmail.com"];
        }

        var subject = $"[هشدار سفارش جدید] سفارش #{orderNumber} ثبت شد";
        var body = $@"
<div dir='rtl' style='font-family: Tahoma, Arial, sans-serif; background: #fff; padding: 20px; border: 1px solid #1c211b;'>
    <h3 style='color: #082719;'>یک سفارش جدید در سامانه هیزو ثبت گردید</h3>
    <ul>
        <li><strong>شماره سفارش:</strong> #{orderNumber}</li>
        <li><strong>مشتری:</strong> {customerName}</li>
        <li><strong>مبلغ کل:</strong> {totalAmount:N0} تومان</li>
        <li><strong>زمان:</strong> {DateTime.UtcNow:yyyy/MM/dd HH:mm} UTC</li>
    </ul>
    <p>جهت بررسی و مدیریت به <a href='http://localhost:3000/admin'>پنل مدیریت هیزو</a> مراجعه فرمایید.</p>
</div>";

        var allSent = true;
        foreach (var admin in adminEmails)
        {
            var sent = await SendEmailAsync(admin, subject, body, cancellationToken);
            if (!sent) allSent = false;
        }
        return allSent;
    }

    public async Task<bool> SendAdminOrderFullDetailsEmailAsync(OrderDto order, string customerPhone, CancellationToken cancellationToken = default)
    {
        var adminEmails = _configuration.GetSection("Email:AdminEmails").Get<string[]>();
        if (adminEmails == null || adminEmails.Length == 0)
        {
            adminEmails = ["as.masoud.1868@gmail.com"];
        }

        var frontendUrl = _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:3000";
        var adminDashboardUrl = $"{frontendUrl.TrimEnd('/')}/admin";

        var subject = $"[سفارش جدید] سفارش #{order.OrderNumber} - {order.CustomerName} ({order.Total:N0} تومان)";

        var itemsRows = new StringBuilder();
        var index = 1;
        foreach (var item in order.Items)
        {
            itemsRows.Append($@"
            <tr style='border-bottom: 1px solid #eee;'>
                <td style='padding: 10px; text-align: center; color: #555;'>{index++}</td>
                <td style='padding: 10px; font-weight: bold; color: #161A1D;'>{item.ProductName}</td>
                <td style='padding: 10px; text-align: center; font-family: monospace; color: #777;'>{item.Sku}</td>
                <td style='padding: 10px; text-align: center; font-weight: bold;'>{item.Quantity}</td>
                <td style='padding: 10px; text-align: left; font-family: Tahoma;'>{item.UnitPrice:N0} تومان</td>
                <td style='padding: 10px; text-align: left; font-weight: bold; color: #082719;'>{item.TotalPrice:N0} تومان</td>
            </tr>");
        }

        var bodyHtml = $@"
<!DOCTYPE html>
<html dir='rtl' lang='fa'>
<head>
    <meta charset='utf-8' />
</head>
<body style='margin: 0; padding: 20px; background-color: #F7F5EE; font-family: Tahoma, Segoe UI, Arial, sans-serif; direction: rtl;'>
    <div style='max-width: 680px; margin: auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; border: 1px solid #dfc79b; box-shadow: 0 4px 20px rgba(0,0,0,0.06);'>
        
        <!-- Header -->
        <div style='background-color: #082719; color: #ffffff; padding: 24px; text-align: center;'>
            <h1 style='margin: 0; font-size: 20px; color: #dfc79b; letter-spacing: 1px;'>استودیو طراحی و ساخت دیجیتال هیزو</h1>
            <p style='margin: 6px 0 0; font-size: 13px; color: #EDE8DC;'>گزارش ثبت سفارش جدید در سامانه فروشگاه</p>
        </div>

        <div style='padding: 24px;'>
            
            <!-- Order Meta Badge -->
            <div style='background-color: #EDE8DC; border-radius: 12px; padding: 14px 18px; margin-bottom: 20px; display: flex; justify-content: space-between; align-items: center;'>
                <div>
                    <span style='font-size: 13px; color: #5A626A;'>شماره فاکتور:</span>
                    <strong style='font-size: 16px; color: #082719; font-family: monospace; margin-right: 6px;'>#{order.OrderNumber}</strong>
                </div>
                <div style='text-align: left;'>
                    <span style='font-size: 12px; color: #5A626A;'>تاریخ ثبت:</span>
                    <strong style='font-size: 12px; color: #161A1D;'>{DateTime.UtcNow:yyyy/MM/dd - HH:mm} UTC</strong>
                </div>
            </div>

            <!-- Customer & Shipping Information -->
            <div style='display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 20px;'>
                
                <div style='background: #FAF7F0; border: 1px solid #dfc79b/50; border-radius: 10px; padding: 14px;'>
                    <h4 style='margin: 0 0 10px; font-size: 13px; color: #082719; border-bottom: 1px solid #e0dacb; pb: 6px;'>مشخصات خریدار</h4>
                    <p style='margin: 4px 0; font-size: 12px; color: #333;'><strong>نام و نام‌خانوادگی:</strong> {order.CustomerName}</p>
                    <p style='margin: 4px 0; font-size: 12px; color: #333;'><strong>شماره تماس:</strong> <span dir='ltr' style='font-family: monospace;'>{customerPhone}</span></p>
                    <p style='margin: 4px 0; font-size: 12px; color: #333;'><strong>ایمیل:</strong> <span dir='ltr'>{order.CustomerEmail}</span></p>
                </div>

                <div style='background: #FAF7F0; border: 1px solid #dfc79b/50; border-radius: 10px; padding: 14px;'>
                    <h4 style='margin: 0 0 10px; font-size: 13px; color: #082719; border-bottom: 1px solid #e0dacb; pb: 6px;'>آدرس تحویل گیرنده</h4>
                    <p style='margin: 4px 0; font-size: 12px; color: #333;'><strong>استان و شهر:</strong> {order.ShippingAddress.State} - {order.ShippingAddress.City}</p>
                    <p style='margin: 4px 0; font-size: 12px; color: #333;'><strong>نشانی دقیق:</strong> {order.ShippingAddress.Street}</p>
                    <p style='margin: 4px 0; font-size: 12px; color: #333;'><strong>کد پستی:</strong> <span dir='ltr'>{order.ShippingAddress.ZipCode}</span></p>
                </div>

            </div>

            <!-- Order Items Table -->
            <h4 style='margin: 20px 0 10px; font-size: 14px; color: #082719;'>اقلام سفارش داده شده ({order.Items.Count} قلم):</h4>
            <table style='width: 100%; border-collapse: collapse; font-size: 12px; background: #fff; border: 1px solid #eee; border-radius: 8px; overflow: hidden;'>
                <thead>
                    <tr style='background-color: #082719; color: #dfc79b;'>
                        <th style='padding: 10px; text-align: center;'>#</th>
                        <th style='padding: 10px; text-align: right;'>نام محصول</th>
                        <th style='padding: 10px; text-align: center;'>کد SKU</th>
                        <th style='padding: 10px; text-align: center;'>تعداد</th>
                        <th style='padding: 10px; text-align: left;'>قیمت واحد</th>
                        <th style='padding: 10px; text-align: left;'>قیمت کل</th>
                    </tr>
                </thead>
                <tbody>
                    {itemsRows}
                </tbody>
            </table>

            <!-- Financial Summary -->
            <div style='margin-top: 20px; background-color: #FAF7F0; padding: 16px; border-radius: 10px; border: 1px solid #e5dfd2;'>
                <table style='width: 100%; font-size: 13px;'>
                    <tr>
                        <td style='color: #555; padding: 4px 0;'>جمع ارزش اقلام:</td>
                        <td style='text-align: left; font-weight: bold; padding: 4px 0;'>{order.Subtotal:N0} تومان</td>
                    </tr>
                    <tr>
                        <td style='color: #555; padding: 4px 0;'>هزینه بسته‌بندی و ارسال:</td>
                        <td style='text-align: left; font-weight: bold; padding: 4px 0;'>{(order.ShippingCost > 0 ? $"{order.ShippingCost:N0} تومان" : "رایگان")}</td>
                    </tr>
                    <tr style='border-top: 2px solid #dfc79b;'>
                        <td style='color: #082719; font-weight: bold; font-size: 15px; padding-top: 10px;'>مبلغ کل فاکتور:</td>
                        <td style='text-align: left; font-weight: bold; font-size: 16px; color: #082719; padding-top: 10px;'>{order.Total:N0} تومان</td>
                    </tr>
                </table>
            </div>

            <!-- Call to Action -->
            <div style='text-align: center; margin-top: 28px;'>
                <a href='{adminDashboardUrl}' style='background-color: #082719; color: #dfc79b; text-decoration: none; font-size: 14px; font-weight: bold; padding: 12px 28px; border-radius: 8px; display: inline-block; box-shadow: 0 2px 8px rgba(8,39,25,0.2);'>
                    ورود به پنل مدیریت جهت بررسی سفارش
                </a>
            </div>

        </div>

        <!-- Footer -->
        <div style='background: #FAF7F0; border-top: 1px solid #dfc79b/40; padding: 16px; text-align: center; font-size: 11px; color: #737373;'>
            این پیام به صورت خودکار از سامانه مدیریت سفارشات استودیو طراحی و ساخت هیزو ارسال شده است.
        </div>

    </div>
</body>
</html>";

        var allSent = true;
        foreach (var admin in adminEmails)
        {
            var sent = await SendEmailAsync(admin, subject, bodyHtml, cancellationToken);
            if (!sent) allSent = false;
        }
        return allSent;
    }

    public async Task<bool> SendCustomerOrderStatusEmailAsync(
        string to,
        string customerName,
        string orderNumber,
        string statusTitle,
        string statusDescription,
        OrderDto order,
        CancellationToken cancellationToken = default)
    {
        var subject = $"وضعیت سفارش #{orderNumber} شما در استودیو هیزو: «{statusTitle}»";
        var frontendUrl = _configuration["Cors:AllowedOrigins:0"] ?? "http://localhost:3000";
        var trackOrderUrl = $"{frontendUrl.TrimEnd('/')}/orders";

        var itemsRows = new StringBuilder();
        foreach (var item in order.Items)
        {
            itemsRows.Append($@"
            <li style='padding: 6px 0; color: #333; font-size: 13px;'>
                <span>{item.ProductName}</span>
                <span style='color: #777; margin: 0 4px;'>×</span>
                <strong>{item.Quantity}</strong>
                <span style='color: #082719; margin-right: 8px;'>({item.TotalPrice:N0} تومان)</span>
            </li>");
        }

        var bodyHtml = $@"
<!DOCTYPE html>
<html dir='rtl' lang='fa'>
<head>
    <meta charset='utf-8' />
</head>
<body style='margin: 0; padding: 20px; background-color: #F7F5EE; font-family: Tahoma, Segoe UI, Arial, sans-serif; direction: rtl;'>
    <div style='max-width: 600px; margin: auto; background-color: #ffffff; border-radius: 16px; overflow: hidden; border: 1px solid #dfc79b; box-shadow: 0 4px 16px rgba(0,0,0,0.05);'>
        
        <!-- Header -->
        <div style='background-color: #082719; color: #ffffff; padding: 22px; text-align: center;'>
            <h2 style='margin: 0; font-size: 18px; color: #dfc79b;'>استودیو طراحی و ساخت دیجیتال هیزو</h2>
            <p style='margin: 4px 0 0; font-size: 12px; color: #EDE8DC;'>اطلاع‌رسانی وضعیت سفارش #{orderNumber}</p>
        </div>

        <div style='padding: 24px;'>
            
            <p style='font-size: 14px; color: #161A1D; margin-top: 0;'>
                <strong>{customerName}</strong> گرامی، سلام؛
            </p>

            <!-- Status Banner -->
            <div style='background: #EDE8DC; border: 1px solid #dfc79b; border-radius: 12px; padding: 18px; text-align: center; margin: 18px 0;'>
                <span style='font-size: 12px; color: #5A626A; display: block; margin-bottom: 6px;'>وضعیت جدید سفارش شما:</span>
                <span style='display: inline-block; background: #082719; color: #ffffff; font-size: 16px; font-weight: bold; padding: 8px 20px; border-radius: 20px;'>
                    {statusTitle}
                </span>
                <p style='margin: 12px 0 0; font-size: 13px; color: #161A1D; line-height: 1.6;'>
                    {statusDescription}
                </p>
            </div>

            <!-- Order Summary -->
            <div style='background-color: #FAF7F0; border-radius: 10px; padding: 16px; margin: 20px 0;'>
                <h4 style='margin: 0 0 10px; font-size: 13px; color: #082719;'>خلاصه سفارش #{orderNumber}</h4>
                <ul style='margin: 0; padding-right: 20px;'>
                    {itemsRows}
                </ul>
                <div style='border-top: 1px solid #dfc79b/60; margin-top: 12px; padding-top: 10px; display: flex; justify-content: space-between; font-size: 13px;'>
                    <span>مبلغ کل سفارش:</span>
                    <strong style='color: #082719;'>{order.Total:N0} تومان</strong>
                </div>
                <div style='margin-top: 6px; font-size: 12px; color: #555;'>
                    <span>نشانی تحویل:</span>
                    <span>{order.ShippingAddress.City}، {order.ShippingAddress.Street}</span>
                </div>
            </div>

            <!-- Link -->
            <div style='text-align: center; margin-top: 24px;'>
                <a href='{trackOrderUrl}' style='background-color: #082719; color: #dfc79b; text-decoration: none; font-size: 13px; font-weight: bold; padding: 10px 24px; border-radius: 8px; display: inline-block;'>
                    مشاهده جزئیات سفارش در حساب کاربری
                </a>
            </div>

        </div>

        <!-- Footer -->
        <div style='background: #FAF7F0; border-top: 1px solid #dfc79b/40; padding: 14px; text-align: center; font-size: 11px; color: #737373;'>
            پشتیبانی استودیو هیزو | تلفن: ۰۹۱۰۱۸۵۴۰۷۶ | وب‌سایت: hizu.ir
        </div>

    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, bodyHtml, cancellationToken);
    }
}
