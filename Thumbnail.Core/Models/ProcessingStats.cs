namespace Thumbnail.Core.Models;

public class ProcessingStats
{
    public int FilesScanned;
    public int VideosDetected;
    public int Generated;
    public int Skipped;
    public int Unsupported;
    public int Failed;
    public TimeSpan TotalDuration { get; set; }

    public void PrintSummary()
    {
        Console.WriteLine("\nProcessing complete!");
        Console.WriteLine("--------------------------------");
        Console.WriteLine("Thumbnail Generation Summary");
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"Files scanned:      {FilesScanned}");
        Console.WriteLine($"Videos detected:    {VideosDetected}");
        Console.WriteLine($"Generated:          {Generated}");
        Console.WriteLine($"Skipped:            {Skipped}");
        Console.WriteLine($"Unsupported:        {Unsupported}");
        Console.WriteLine($"Failed:             {Failed}");
        Console.WriteLine($"Total Duration:     {TotalDuration.TotalSeconds:F1} sec");
        Console.WriteLine("--------------------------------");
    }
}
