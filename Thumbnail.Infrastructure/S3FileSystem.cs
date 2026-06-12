using Amazon.S3;
using Amazon.S3.Model;
using Thumbnail.Core.Interfaces;

namespace Thumbnail.Infrastructure;

public class S3FileInfo : IFileInfo
{
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsDirectory { get; set; }
}

public class S3FileSystem : IFileSystem
{
    private readonly IAmazonS3 _s3Client;

    public S3FileSystem(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string path)
    {
        // Expected path format: s3://bucket-name/prefix/
        var uri = new Uri(path);
        
        var bucketName = uri.Host;
        var prefix = uri.AbsolutePath.TrimStart('/');

        var request = new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = prefix
        };

        var response = await _s3Client.ListObjectsV2Async(request);
        
        return response.S3Objects?.Select(o => $"s3://{bucketName}/{o.Key}") ?? Enumerable.Empty<string>();
    }

    public async Task<IFileInfo> OpenFileAsync(string path)
    {
        var uri = new Uri(path);
        var bucketName = uri.Host;
        var key = uri.AbsolutePath.TrimStart('/');

        var response = await _s3Client.GetObjectMetadataAsync(bucketName, key);
        return new S3FileInfo
        {
            Name = Path.GetFileName(key),
            Size = response.ContentLength,
            IsDirectory = false // S3 doesn't have directories, but we'll assume files for now
        };
    }

    public async Task<bool> FileExistsAsync(string path)
    {
        var uri = new Uri(path);
        var bucketName = uri.Host;
        var key = uri.AbsolutePath.TrimStart('/');

        try
        {
            await _s3Client.GetObjectMetadataAsync(bucketName, key);
            return true;
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public Task EnsureDirectoryExistsAsync(string path)
    {
        // S3 doesn't need to ensure directories, they are created on upload
        return Task.CompletedTask;
    }
}
