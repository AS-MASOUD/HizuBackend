using FluentAssertions;
using Hizu.Application.Features.Contact.Commands;
using Hizu.Application.Features.Contact.Queries;
using Hizu.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hizu.UnitTests;

public class ContactMessageTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    public ContactMessageTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateContactMessage_ShouldPersistCorrectly_AndDefaultToUnread()
    {
        // Arrange
        var handler = new CreateContactMessageCommandHandler(_context);
        var command = new CreateContactMessageCommand
        {
            Name = "علی رضایی",
            Email = "ali@example.com",
            PhoneNumber = "09121234567",
            Subject = "استعلام پرینت رزینی",
            Message = "سلام، امکان تولید ۵۰ قطعه رزینی با دقت ۲۰ میکرون وجود دارد؟"
        };

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("علی رضایی");
        result.Email.Should().Be("ali@example.com");
        result.IsRead.Should().BeFalse();

        var entity = await _context.ContactMessages.FindAsync(result.Id);
        entity.Should().NotBeNull();
        entity!.Name.Should().Be("علی رضایی");
        entity.IsRead.Should().BeFalse();
    }

    [Fact]
    public async Task MarkContactMessageRead_ShouldUpdateReadStatus()
    {
        // Arrange
        var createHandler = new CreateContactMessageCommandHandler(_context);
        var created = await createHandler.Handle(new CreateContactMessageCommand
        {
            Name = "تست کاربر",
            Email = "user@test.com",
            Message = "متن پیام تستی"
        }, CancellationToken.None);

        var readHandler = new MarkContactMessageReadCommandHandler(_context);

        // Act
        var success = await readHandler.Handle(new MarkContactMessageReadCommand(created.Id, true), CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        var entity = await _context.ContactMessages.FindAsync(created.Id);
        entity!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task ReplyContactMessage_ShouldSaveAdminNotes_AndMarkAsRead()
    {
        // Arrange
        var createHandler = new CreateContactMessageCommandHandler(_context);
        var created = await createHandler.Handle(new CreateContactMessageCommand
        {
            Name = "مهندس محمدی",
            Email = "mohammadi@test.com",
            Message = "استعلام تراشکاری CNC شمش برنج"
        }, CancellationToken.None);

        var replyHandler = new ReplyContactMessageCommandHandler(_context);

        // Act
        var success = await replyHandler.Handle(new ReplyContactMessageCommand(created.Id, "با مشتری تماس گرفته شد و فایل STEP درخواست گردید."), CancellationToken.None);

        // Assert
        success.Should().BeTrue();
        var entity = await _context.ContactMessages.FindAsync(created.Id);
        entity!.AdminNotes.Should().Be("با مشتری تماس گرفته شد و فایل STEP درخواست گردید.");
        entity.IsRead.Should().BeTrue();
        entity.RepliedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetContactMessagesQuery_ShouldSupportSearchAndFiltering()
    {
        // Arrange
        var createHandler = new CreateContactMessageCommandHandler(_context);
        await createHandler.Handle(new CreateContactMessageCommand
        {
            Name = "رضا کریمی",
            Email = "reza@test.com",
            Subject = "برش لیزر",
            Message = "ورق آهن ۴ میل"
        }, CancellationToken.None);

        var msg2 = await createHandler.Handle(new CreateContactMessageCommand
        {
            Name = "سارا امینی",
            Email = "sara@test.com",
            Subject = "پرینتر صنعتی",
            Message = "مدل سه بعدی فیلامنت نایلون کربن"
        }, CancellationToken.None);

        // Mark msg2 as read
        var readHandler = new MarkContactMessageReadCommandHandler(_context);
        await readHandler.Handle(new MarkContactMessageReadCommand(msg2.Id, true), CancellationToken.None);

        var queryHandler = new GetContactMessagesQueryHandler(_context);

        // Act: Filter unread only
        var unreadResult = await queryHandler.Handle(new GetContactMessagesQuery { IsRead = false }, CancellationToken.None);

        // Assert
        unreadResult.Items.Should().HaveCount(1);
        unreadResult.Items[0].Name.Should().Be("رضا کریمی");

        // Act: Search keyword
        var searchResult = await queryHandler.Handle(new GetContactMessagesQuery { Search = "نایلون" }, CancellationToken.None);
        searchResult.Items.Should().HaveCount(1);
        searchResult.Items[0].Name.Should().Be("سارا امینی");
    }
}
