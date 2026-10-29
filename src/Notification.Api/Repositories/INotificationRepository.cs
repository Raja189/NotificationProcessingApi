using Notification.Api.Models;

namespace Notification.Api.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Models.Notification notification, CancellationToken cancellationToken);
    Task<Models.Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Models.Notification>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken);
    Task<Models.Notification?> FindRecentDuplicateAsync(string fingerprint, DateTimeOffset createdAfter, CancellationToken cancellationToken);
    Task<IReadOnlyList<Models.Notification>> GetDuePendingAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
