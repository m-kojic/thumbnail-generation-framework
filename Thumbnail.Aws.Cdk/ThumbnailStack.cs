using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SQS;
using Constructs;
using System.Collections.Generic;

namespace Thumbnail.Aws.Cdk
{
    public class ThumbnailStack : Stack
    {
        internal ThumbnailStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            // 5. DLQs for each Lambda
            var scannerDLQ = new Queue(this, "ScannerDLQ", new QueueProps
            {
                QueueName = "thumbnail-scanner-dlq",
                RetentionPeriod = Duration.Days(7)
            });

            var jobsDLQ = new Queue(this, "JobsDLQ", new QueueProps
            {
                QueueName = "thumbnail-jobs-dlq",
                RetentionPeriod = Duration.Days(7)
            });

            // 1. SQS which will receive a request (Input S3 bucket url, Output S3 bucket url)
            var scannerInputQueue = new Queue(this, "ScannerInputQueue", new QueueProps
            {
                QueueName = "thumbnail-scanner-input",
                VisibilityTimeout = Duration.Minutes(5),
                DeadLetterQueue = new DeadLetterQueue
                {
                    MaxReceiveCount = 3,
                    Queue = scannerDLQ
                }
            });

            // 3. Another SQS where the previously created Lambda will publish thumbnail generations jobs
            var jobsQueue = new Queue(this, "JobsQueue", new QueueProps
            {
                QueueName = "thumbnail-jobs",
                VisibilityTimeout = Duration.Minutes(15),
                DeadLetterQueue = new DeadLetterQueue
                {
                    MaxReceiveCount = 3,
                    Queue = jobsDLQ
                }
            });

            // 2. Lambda function triggered when a message lands on SQS (Scanner)
            var scannerLambda = new Function(this, "ScannerFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Architecture = Architecture.ARM_64,
                Handler = "Thumbnail.Aws.Scanner::Thumbnail.Aws.Scanner.Function::FunctionHandler",
                Code = Code.FromAsset("../Thumbnail.Aws.Scanner/bin/Release/net9.0/linux-arm64/publish"),
                Environment = new Dictionary<string, string>
                {
                    { "JOBS_QUEUE_URL", jobsQueue.QueueUrl },
                    { "MAX_SCANNER_WORKERS", "4" },
                    { "SUPPORTED_EXTENSIONS", ".mp4,.mov,.avi,.mkv,.webm" },
                    { "DEPLOY_VERSION", DateTime.UtcNow.ToString("yyyyMMddHHmmss") }
                },
                Timeout = Duration.Minutes(5),
                MemorySize = 512
            });

            scannerLambda.AddEventSource(new SqsEventSource(scannerInputQueue));

            // 1. Reference the public community FFmpeg layer ARN
            var ffmpegLayer = LayerVersion.FromLayerVersionArn(
                this, 
                "FfmpegLayer", 
                "arn:aws:lambda:us-east-1:145266761615:layer:ffmpeg:4"
            );

            // 4. Another Lambda function triggered when a thumbnail generation job lands on SQS (Worker)
            var workerLambda = new Function(this, "WorkerFunction", new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                Architecture = Architecture.ARM_64,
                Handler = "Thumbnail.Aws.Worker::Thumbnail.Aws.Worker.Function::FunctionHandler",
                Code = Code.FromAsset("../Thumbnail.Aws.Worker/bin/Release/net9.0/linux-arm64/publish"),
                Environment = new Dictionary<string, string>
                {
                    { "FFMPEG_PATH", "/opt/ffmpeg" },
                    { "MAX_THUMBNAIL_WORKERS", "4" },
                    { "SKIP_EXISTING", "true" },
                    { "DEPLOY_VERSION", DateTime.UtcNow.ToString("yyyyMMddHHmmss") }
                },
                Timeout = Duration.Minutes(15),
                MemorySize = 2048,
                Layers = new[] { ffmpegLayer }
            });

            workerLambda.AddEventSource(new SqsEventSource(jobsQueue));

            // 6. IAM roles and policies
            
            // Scanner needs to:
            // - Read from Input SQS (Done by AddEventSource)
            // - Publish to Jobs SQS
            jobsQueue.GrantSendMessages(scannerLambda);
            
            // - Read from S3 (ListBucket and GetObject for scanning)
            scannerLambda.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[] { "s3:ListBucket", "s3:GetObject" },
                Resources = new[] { "arn:aws:s3:::*", "arn:aws:s3:::*/*" }
            }));

            // Worker needs to:
            // - Read from Jobs SQS (Done by AddEventSource)
            // - Read from S3 (Download video)
            // - Write to S3 (Upload thumbnail)
            workerLambda.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
            {
                Effect = Effect.ALLOW,
                Actions = new[] { "s3:GetObject", "s3:PutObject" },
                Resources = new[] { "arn:aws:s3:::*", "arn:aws:s3:::*/*" }
            }));
        }
    }
}
