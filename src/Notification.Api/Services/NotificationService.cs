using System.Security.Cryptography;
using System.Text;
using Notification.Api.DTOs;
using Notification.Api.Models;
using Notification.Api.Providers;
using Notification.Api.Repositories;

namespace Notification.Api.Services;

public sealed class NotificationService(
    INotificationRepository repository,
    IEmailNotificationProvider provider,
    TimeProvider timeProvider,
    ILogger<NotificationService> logger) : INotificationService
{
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(5);

    public async Task<CreateNotificationResult> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        // API model validation handles required fields and email syntax; enforce business enums and time here.
        if (!Enum.TryParse<NotificationType>(request.Type, true, out var type) || type != NotificationType.Email)
            throw new ArgumentException("Unsupported notification type. Only Email is supported.");
        if (!Enum.TryParse<NotificationPriority>(request.Priority, true, out var priority) ||
            !Enum.IsDefined(priority))
            throw new ArgumentException("Priority must be Low, Normal, or High.");
        if (string.IsNullOrWhiteSpace(request.CustomerId)) throw new ArgumentException("CustomerId is required.");
        if (string.IsNullOrWhiteSpace(request.Recipient) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(request.Recipient))
            throw new ArgumentException("Recipient must be a valid email address.");
        if (string.IsNullOrWhiteSpace(request.Message)) throw new ArgumentException("Message is required.");

        var now = timeProvider.GetUtcNow();
        if (request.RequestedDeliveryTime < now)
            throw new ArgumentException("RequestedDeliveryTime cannot be in the past.");

        var fingerprint = CreateFingerprint(request, type, priority);
        var duplicate = await repository.FindRecentDuplicateAsync(fingerprint, now - DuplicateWindow, cancellationToken);
        if (duplicate is not null)
            return new CreateNotificationResult(NotificationResponse.FromEntity(duplicate), true);

        var notification = new Models.Notification
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId.Trim(),
            Type = type,
            Recipient = request.Recipient.Trim(),
            Subject = request.Subject?.Trim(),
            Message = request.Message.Trim(),
            Priority = priority,
            RequestedDeliveryTime = request.RequestedDeliveryTime,
            CreatedAt = now,
            Status = NotificationStatus.Pending,
            RequestFingerprint = fingerprint
        };
        await repository.AddAsync(notification, cancellationToken);
        return new CreateNotificationResult(NotificationResponse.FromEntity(notification), false);
    }

    public async Task<NotificationResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var notification = await repository.GetByIdAsync(id, cancellationToken);
        return notification is null ? null : NotificationResponse.FromEntity(notification);
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetByCustomerAsync(string customerId, CancellationToken cancellationToken)
    {
        var notifications = await repository.GetByCustomerAsync(customerId, cancellationToken);
        return notifications.Select(NotificationResponse.FromEntity).ToArray();
    }

    public async Task<ProcessingSummary> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var due = await repository.GetDuePendingAsync(timeProvider.GetUtcNow(), cancellationToken);
        var sent = 0;
        var failed = 0;
        foreach (var notification in due)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Persist Processing before calling the external dependency.
            notification.Status = NotificationStatus.Processing;
            await repository.SaveChangesAsync(cancellationToken);
            try
            {
                await provider.SendAsync(notification, cancellationToken);
                notification.Status = NotificationStatus.Sent;
                notification.FailureReason = null;
                sent++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Preserve the in-progress state if the host is shutting down; production systems
                // should recover stale Processing records on startup.
                throw;
            }
            catch (Exception ex)
            {
                notification.Status = NotificationStatus.Failed;
                notification.FailureReason = ex.Message;
                failed++;
                logger.LogError(ex, "Notification {NotificationId} failed to send", notification.Id);
            }
            await repository.SaveChangesAsync(cancellationToken);
        }
        return new ProcessingSummary(due.Count, sent, failed);
    }

    private static string CreateFingerprint(CreateNotificationRequest request, NotificationType type, NotificationPriority priority)
    {
        var canonical = string.Join("\n", request.CustomerId.Trim(), type.ToString(), request.Recipient.Trim().ToLowerInvariant(),
            request.Subject?.Trim() ?? string.Empty, request.Message.Trim(), priority.ToString(),
            request.RequestedDeliveryTime.ToUniversalTime().ToString("O"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
