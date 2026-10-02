namespace Triso.Application.Ports.Notifications;
public sealed record NotificationMessage(string Destination, string Type, string Content);
public interface INotificationSender { Task SendAsync(NotificationMessage message, CancellationToken ct); }
