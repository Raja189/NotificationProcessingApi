using Notification.Api.Models;

namespace Notification.Api.DTOs;

public sealed record NotificationResponse(
    Guid Id,
    string CustomerId,
    string Type,
    string Recipient,
    string? Subject,
    string Message,
    string Priority,
    DateTimeOffset RequestedDeliveryTime,
    DateTimeOffset CreatedAt,
    string Status,
    string? FailureReason)
{
    public static NotificationResponse FromEntity(Notification.Api.Models.Notification n) => new(
        n.Id, n.CustomerId, n.Type.ToString(), n.Recipient, n.Subject, n.Message,
        n.Priority.ToString(), n.RequestedDeliveryTime, n.CreatedAt, n.Status.ToString(), n.FailureReason);
}
