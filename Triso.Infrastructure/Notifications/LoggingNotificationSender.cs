using Microsoft.Extensions.Logging;
using Triso.Application.Ports.Notifications;
namespace Triso.Infrastructure.Notifications;
public sealed class LoggingNotificationSender(ILogger<LoggingNotificationSender> logger) : INotificationSender
{
    public Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        logger.LogInformation("Notification queued for {Destination}; type {Type}", message.Destination, message.Type);
        return Task.CompletedTask;
    }
}
