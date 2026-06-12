namespace Thumbnail.Core.Interfaces;

public interface IQueuePublisher
{
    Task PublishAsync<T>(string queueUrl, T message);
}
