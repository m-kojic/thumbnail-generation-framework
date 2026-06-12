using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Transfer;
using Thumbnail.Core.Interfaces;
using Thumbnail.Infrastructure;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Thumbnail.Aws.Worker;

public class Function
{
    private readonly IAmazonS3 _s3Client;
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly int _maxThumbnailWorkers;
    private readonly bool _skipExisting;

    public Function()
    {
        _s3Client = new AmazonS3Client();
        // In production, ffmpeg is typically provided via a Lambda Layer.
        // It is extracted to /opt, so /opt/bin/ffmpeg is the standard path.
        var ffmpegPath = Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "/opt/bin/ffmpeg";
        _thumbnailGenerator = new FfmpegGenerator(ffmpegPath);
        
        _maxThumbnailWorkers = int.TryParse(Environment.GetEnvironmentVariable("MAX_THUMBNAIL_WORKERS"), out var max) ? max : 4;
        _skipExisting = bool.TryParse(Environment.GetEnvironmentVariable("SKIP_EXISTING"), out var skip) && skip;
    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _maxThumbnailWorkers
        };

        await Parallel.ForEachAsync(evnt.Records, parallelOptions, async (message, ct) =>
        {
            await ProcessMessageAsync(message, context);
        });
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        var job = JsonSerializer.Deserialize<ThumbnailJob>(message.Body);
        if (job == null) return;

        context.Logger.LogInformation($"Processing video: {job.VideoS3Url}");

        var videoLocalPath = Path.Combine("/tmp", Path.GetRandomFileName());
        var thumbnailLocalPath = Path.Combine("/tmp", Path.GetRandomFileName() + ".jpg");

        try
        {
            // 3. Setup output details (needed early for skip check)
            var outputUri = new Uri(job.OutputS3Url);
            var videoUri = new Uri(job.VideoS3Url);
            var bucketName = outputUri.Host;
            var prefix = outputUri.AbsolutePath.TrimStart('/');
            var fileName = Path.GetFileNameWithoutExtension(videoUri.AbsolutePath) + ".jpg";
            var outputKey = Path.Combine(prefix, fileName);

            context.Logger.LogInformation($"Parsed Output: Bucket={bucketName}, Key={outputKey}");
            context.Logger.LogInformation($"Parsed Input: Bucket={videoUri.Host}, Key={videoUri.AbsolutePath.TrimStart('/')}");

            // 0. Skip check
            if (_skipExisting && await ExistsInS3Async(bucketName, outputKey))
            {
                context.Logger.LogInformation($"Skipping existing thumbnail: s3://{bucketName}/{outputKey}");
                return;
            }

            // 1. Download video
            await DownloadFromS3Async(job.VideoS3Url, videoLocalPath);

            // 2. Generate thumbnail
            await _thumbnailGenerator.GenerateThumbnailAsync(videoLocalPath, thumbnailLocalPath);

            // 4. Upload thumbnail
            await UploadToS3Async(thumbnailLocalPath, bucketName, outputKey);
            context.Logger.LogInformation($"Uploaded thumbnail to s3://{bucketName}/{outputKey}");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing {job.VideoS3Url}: {ex.Message}");
            throw; 
        }
        finally
        {
            // 5. Cleanup
            if (File.Exists(videoLocalPath)) File.Delete(videoLocalPath);
            if (File.Exists(thumbnailLocalPath)) File.Delete(thumbnailLocalPath);
        }
    }

    private async Task<bool> ExistsInS3Async(string bucketName, string key)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(bucketName, key);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task DownloadFromS3Async(string s3Url, string localPath)
    {
        var uri = new Uri(s3Url);
        var bucketName = uri.Host;
        var key = uri.AbsolutePath.TrimStart('/');

        using var response = await _s3Client.GetObjectAsync(bucketName, key);
        await response.WriteResponseStreamToFileAsync(localPath, false, default);
    }

    private async Task UploadToS3Async(string localPath, string bucketName, string key)
    {
        var fileTransferUtility = new TransferUtility(_s3Client);
        await fileTransferUtility.UploadAsync(localPath, bucketName, key);
    }
}

public class ThumbnailJob
{
    public string VideoS3Url { get; set; } = string.Empty;
    public string OutputS3Url { get; set; } = string.Empty;
}
