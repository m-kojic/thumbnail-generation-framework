using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.SQS;
using Thumbnail.Core.Interfaces;
using Thumbnail.Infrastructure;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Thumbnail.Aws.Scanner;

public class Function
{
    private readonly IFileSystem _s3FileSystem;
    private readonly IQueuePublisher _queuePublisher;
    private readonly string _jobsQueueUrl;
    private readonly int _maxScannerWorkers;
    private readonly string[] _supportedExtensions;

    public Function()
    {
        var s3Client = new AmazonS3Client();
        var sqsClient = new AmazonSQSClient();
        _s3FileSystem = new S3FileSystem(s3Client);
        _queuePublisher = new SqsQueuePublisher(sqsClient);
        _jobsQueueUrl = Environment.GetEnvironmentVariable("JOBS_QUEUE_URL") ?? throw new Exception("JOBS_QUEUE_URL missing");
        
        _maxScannerWorkers = int.TryParse(Environment.GetEnvironmentVariable("MAX_SCANNER_WORKERS"), out var max) ? max : 4;
        _supportedExtensions = Environment.GetEnvironmentVariable("SUPPORTED_EXTENSIONS")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) 
                               ?? new[] { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        if (evnt?.Records == null) return;

        foreach (var message in evnt.Records)
        {
            if (string.IsNullOrWhiteSpace(message?.Body)) continue;
            
            try
            {
                await ProcessMessageAsync(message, context);
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing message: {ex.Message}. Body: {message.Body}");
            }
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processed message {message.Body}");
        ScannerRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ScannerRequest>(message.Body);
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"Failed to deserialize message body: {ex.Message}");
            throw;
        }

        if (request == null || string.IsNullOrWhiteSpace(request.InputS3Url))
        {
            context.Logger.LogWarning($"Invalid or empty request body: {message.Body}");
            return;
        }

        var files = await _s3FileSystem.ListFilesAsync(request.InputS3Url);
        
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxScannerWorkers
        };

        await Parallel.ForEachAsync(files, parallelOptions, async (file, ct) =>
        {
            if (file == null) return;
            var extension = Path.GetExtension(file)?.ToLower();
            if (extension != null && _supportedExtensions != null && _supportedExtensions.Contains(extension))
            {
                var job = new ThumbnailJob
                {
                    VideoS3Url = file,
                    OutputS3Url = request.OutputS3Url
                };
                await _queuePublisher.PublishAsync(_jobsQueueUrl, job);
            }
        });
    }
}

public class ScannerRequest
{
    public string InputS3Url { get; set; } = string.Empty;
    public string OutputS3Url { get; set; } = string.Empty;
}

public class ThumbnailJob
{
    public string VideoS3Url { get; set; } = string.Empty;
    public string OutputS3Url { get; set; } = string.Empty;
}
