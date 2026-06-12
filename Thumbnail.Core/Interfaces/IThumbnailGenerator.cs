namespace Thumbnail.Core.Interfaces;

public interface IThumbnailGenerator
{
    Task GenerateThumbnailAsync(string videoPath, string outputPath);
}
