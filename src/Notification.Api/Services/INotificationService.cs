using Notification.Api.DTOs;

namespace Notification.Api.Services;

public sealed record CreateNotificationResult(NotificationResponse Notification, bool IsDuplicate);
public sealed record ProcessingSummary(int Processed, int Sent, int Failed);

public interface INotificationService
{
    Task<CreateNotificationResult> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken);
    Task<NotificationResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationResponse>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken);
    Task<ProcessingSummary> ProcessPendingAsync(CancellationToken cancellationToken);
}
