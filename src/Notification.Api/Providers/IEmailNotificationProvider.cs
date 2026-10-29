using Notification.Api.Models;

namespace Notification.Api.Providers;

public interface IEmailNotificationProvider
{
    Task SendAsync(Models.Notification notification, CancellationToken cancellationToken);
}
