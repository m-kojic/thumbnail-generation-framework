namespace Thumbnail.Core.Models;

public class ThumbnailOptions
{
    public int MaxScannerWorkers { get; set; } = 4;
    public int MaxThumbnailWorkers { get; set; } = Environment.ProcessorCount;
    public bool SkipExisting { get; set; } = true;
    public string[] SupportedExtensions { get; set; } = { ".mp4", ".mov", ".avi", ".mkv", ".webm" };
}
