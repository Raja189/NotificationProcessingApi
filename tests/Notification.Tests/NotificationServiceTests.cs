using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Notification.Api.Data;
using Notification.Api.DTOs;
using Notification.Api.Models;
using Notification.Api.Providers;
using Notification.Api.Repositories;
using Notification.Api.Services;
using Xunit;
using Models = Notification.Api.Models;

namespace Notification.Tests;

public sealed class NotificationServiceTests
{
    private static (NotificationService Service, NotificationDbContext Db) CreateService(Func<Models.Notification, Exception?>? failure = null)
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new NotificationDbContext(options);
        var repository = new EfNotificationRepository(db);
        var provider = new TestEmailProvider(failure);
        var service = new NotificationService(repository, provider, TimeProvider.System, NullLogger<NotificationService>.Instance);
        return (service, db);
    }

    private static CreateNotificationRequest Request(string recipient = "customer@example.com", string priority = "Normal", DateTimeOffset? delivery = null) => new()
    {
        CustomerId = "CUST-101", Type = "Email", Recipient = recipient, Subject = "Order Confirmed",
        Message = "Your order has been confirmed.", Priority = priority,
        RequestedDeliveryTime = delivery ?? DateTimeOffset.UtcNow.AddMinutes(1)
    };

    [Fact]
    public async Task CreateAsync_StoresPendingNotification()
    {
        var (service, db) = CreateService();
        var result = await service.CreateAsync(Request(), CancellationToken.None);
        Assert.False(result.IsDuplicate);
        Assert.Equal("Pending", result.Notification.Status);
        Assert.Single(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_RejectsInvalidPriority()
    {
        var (service, _) = CreateService();
        var error = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Request(priority: "Urgent"), CancellationToken.None));
        Assert.Contains("Priority", error.Message);
    }

    [Fact]
    public async Task CreateAsync_DetectsDuplicateWithinWindow()
    {
        var (service, db) = CreateService();
        var request = Request();
        var first = await service.CreateAsync(request, CancellationToken.None);
        var second = await service.CreateAsync(request, CancellationToken.None);
        Assert.False(first.IsDuplicate);
        Assert.True(second.IsDuplicate);
        Assert.Equal(first.Notification.Id, second.Notification.Id);
        Assert.Single(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task ProcessPendingAsync_SendsHighPriorityBeforeNormal()
    {
        var (service, db) = CreateService();
        await service.CreateAsync(Request("normal@example.com", "Normal"), CancellationToken.None);
        await service.CreateAsync(Request("high@example.com", "High"), CancellationToken.None);
        // Move scheduled times to the past to make these test notifications due.
        var items = await db.Notifications.ToListAsync();
        foreach (var item in items) item.RequestedDeliveryTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var summary = await service.ProcessPendingAsync(CancellationToken.None);
        Assert.Equal(2, summary.Processed);
        Assert.Equal(2, summary.Sent);
        Assert.Equal("Sent", (await db.Notifications.SingleAsync(n => n.Priority == NotificationPriority.High)).Status.ToString());
    }

    [Fact]
    public async Task ProcessPendingAsync_MarksProviderFailureAsFailedAndContinues()
    {
        var (service, db) = CreateService(n => n.Recipient == "broken@fail.test" ? new InvalidOperationException("provider down") : null);
        await service.CreateAsync(Request("broken@fail.test"), CancellationToken.None);
        await service.CreateAsync(Request("ok@example.com", "Low"), CancellationToken.None);
        foreach (var item in await db.Notifications.ToListAsync()) item.RequestedDeliveryTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var summary = await service.ProcessPendingAsync(CancellationToken.None);
        Assert.Equal(2, summary.Processed);
        Assert.Equal(1, summary.Failed);
        Assert.Equal(1, summary.Sent);
        Assert.Contains(await db.Notifications.ToListAsync(), n => n.Status == NotificationStatus.Failed && n.FailureReason == "provider down");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullWhenMissing()
    {
        var (service, _) = CreateService();
        Assert.Null(await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private sealed class TestEmailProvider(Func<Models.Notification, Exception?>? failure) : IEmailNotificationProvider
    {
        public Task SendAsync(Models.Notification notification, CancellationToken cancellationToken)
        {
            var exception = failure?.Invoke(notification);
            if (exception is not null) throw exception;
            return Task.CompletedTask;
        }
    }
}
