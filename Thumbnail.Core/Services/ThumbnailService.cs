using System.Diagnostics;
using Thumbnail.Core.Interfaces;
using Thumbnail.Core.Models;

namespace Thumbnail.Core.Services;

public class ThumbnailService
{
    private readonly IFileSystem _fileSystem;
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly ThumbnailOptions _options;

    public ThumbnailService(IFileSystem fileSystem, IThumbnailGenerator thumbnailGenerator, ThumbnailOptions? options = null)
    {
        _fileSystem = fileSystem;
        _thumbnailGenerator = thumbnailGenerator;
        _options = options ?? new ThumbnailOptions();
    }

    public async Task<ProcessingStats> ProcessDirectoryAsync(string inputPath, string outputPath)
    {
        var stopwatch = Stopwatch.StartNew();
        var stats = new ProcessingStats();
        
        var files = await _fileSystem.ListFilesAsync(inputPath);
        
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.MaxThumbnailWorkers
        };

        await Parallel.ForEachAsync(files, parallelOptions, async (file, ct) =>
        {
            Interlocked.Increment(ref stats.FilesScanned);
            
            if (IsSupportedVideo(file))
            {
                Interlocked.Increment(ref stats.VideosDetected);
                try
                {
                    var targetThumbnailPath = GetTargetThumbnailPath(inputPath, file, outputPath);
                    
                    if (_options.SkipExisting && await _fileSystem.FileExistsAsync(targetThumbnailPath))
                    {
                        Interlocked.Increment(ref stats.Skipped);
                        return;
                    }

                    // Directory creation is local-only concern, handle it if directory path is available
                    var targetDir = Path.GetDirectoryName(targetThumbnailPath);
                    if (targetDir != null)
                    {
                        await _fileSystem.EnsureDirectoryExistsAsync(targetDir);
                    }
                    
                    await _thumbnailGenerator.GenerateThumbnailAsync(file, targetThumbnailPath);
                    Interlocked.Increment(ref stats.Generated);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref stats.Failed);
                    Console.Error.WriteLine($"Failed to process {file}: {ex.Message}");
                }
            }
            else
            {
                Interlocked.Increment(ref stats.Unsupported);
            }
        });

        stopwatch.Stop();
        stats.TotalDuration = stopwatch.Elapsed;
        return stats;
    }

    private string GetTargetThumbnailPath(string inputRoot, string currentFile, string outputRoot)
    {
        var relativePath = Path.GetRelativePath(inputRoot, currentFile);
        var relativeDir = Path.GetDirectoryName(relativePath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(currentFile);
        
        return Path.Combine(outputRoot, relativeDir, $"{fileName}.jpg");
    }

    private bool IsSupportedVideo(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        return _options.SupportedExtensions.Contains(extension);
    }
}
