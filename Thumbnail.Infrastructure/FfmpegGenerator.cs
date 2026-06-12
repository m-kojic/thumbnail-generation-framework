using System.Diagnostics;
using Thumbnail.Core.Interfaces;

namespace Thumbnail.Infrastructure;

public class FfmpegGenerator : IThumbnailGenerator
{
    private readonly string _ffmpegPath;

    public FfmpegGenerator(string? ffmpegPath = null)
    {
        _ffmpegPath = ffmpegPath ?? "ffmpeg"; // Default to PATH search for Lambda
    }

    public async Task GenerateThumbnailAsync(string videoPath, string outputPath)
    {
        // -y to overwrite output if exists
        // -i input file
        // -ss seek to 1 second
        // -vframes 1 take one frame
        var arguments = $"-y -i \"{videoPath}\" -ss 00:00:01 -vframes 1 \"{outputPath}\"";
        
        using var process = new ProcessWrapper(_ffmpegPath, arguments);
        process.Start();
        
        var exitCode = await process.WaitForCompletionAsync();
        
        if (exitCode != 0)
        {
            var error = process.GetStderr();
            throw new Exception($"FFmpeg failed with exit code {exitCode}. Error: {error}");
        }
    }
}
