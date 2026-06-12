using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Thumbnail.Core.Interfaces;

namespace Thumbnail.Infrastructure;

public class SqsQueuePublisher : IQueuePublisher
{
    private readonly IAmazonSQS _sqsClient;

    public SqsQueuePublisher(IAmazonSQS sqsClient)
    {
        _sqsClient = sqsClient;
    }

    public async Task PublishAsync<T>(string queueUrl, T message)
    {
        var json = JsonSerializer.Serialize(message);
        await _sqsClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = json
        });
    }
}
