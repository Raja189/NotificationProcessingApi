using System.ComponentModel.DataAnnotations;
using Notification.Api.Models;

namespace Notification.Api.DTOs;

public sealed class CreateNotificationRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string CustomerId { get; init; } = string.Empty;

    [Required]
    public string Type { get; init; } = "Email";

    [Required, EmailAddress, StringLength(320)]
    public string Recipient { get; init; } = string.Empty;

    [StringLength(200)]
    public string? Subject { get; init; }

    [Required, StringLength(10000, MinimumLength = 1)]
    public string Message { get; init; } = string.Empty;

    [Required]
    public string Priority { get; init; } = "Normal";

    public DateTimeOffset RequestedDeliveryTime { get; init; }
}
