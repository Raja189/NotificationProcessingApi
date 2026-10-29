using Microsoft.EntityFrameworkCore;
using Notification.Api.Data;
using Notification.Api.Models;

namespace Notification.Api.Repositories;

public sealed class EfNotificationRepository(NotificationDbContext db) : INotificationRepository
{
    public async Task AddAsync(Models.Notification notification, CancellationToken cancellationToken)
    {
        await db.Notifications.AddAsync(notification, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Models.Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Notifications.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Models.Notification>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken) =>
        await db.Notifications.AsNoTracking().Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAt).ToListAsync(cancellationToken);

    public Task<Models.Notification?> FindRecentDuplicateAsync(string fingerprint, DateTimeOffset createdAfter, CancellationToken cancellationToken) =>
        db.Notifications.AsNoTracking().Where(n => n.RequestFingerprint == fingerprint && n.CreatedAt >= createdAfter)
            .OrderByDescending(n => n.CreatedAt).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Models.Notification>> GetDuePendingAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.Notifications.Where(n => n.Status == NotificationStatus.Pending && n.RequestedDeliveryTime <= now)
            .OrderByDescending(n => n.Priority).ThenBy(n => n.RequestedDeliveryTime).ThenBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
