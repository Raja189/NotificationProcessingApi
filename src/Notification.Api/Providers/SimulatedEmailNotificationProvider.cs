using Notification.Api.Models;

namespace Notification.Api.Providers;

/// <summary>Demo provider. Use a real provider implementation for production.</summary>
public sealed class SimulatedEmailNotificationProvider(ILogger<SimulatedEmailNotificationProvider> logger) : IEmailNotificationProvider
{
    public Task SendAsync(Models.Notification notification, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Deterministic failure hook for exercising failure handling in the coding exercise.
        if (notification.Recipient.EndsWith("@fail.test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Simulated email provider failure.");

        logger.LogInformation("SIMULATED EMAIL SENT. NotificationId={NotificationId}, To={Recipient}, Subject={Subject}",
            notification.Id, notification.Recipient, notification.Subject);
        return Task.CompletedTask;
    }
}
