namespace Notification.Api.Models;

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Email;
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTimeOffset RequestedDeliveryTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string RequestFingerprint { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}
