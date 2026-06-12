using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Thumbnail.Core.Interfaces;
using Thumbnail.Core.Models;
using Thumbnail.Core.Services;
using Thumbnail.Infrastructure;

// Setup Configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var options = configuration.Get<ThumbnailOptions>() ?? new ThumbnailOptions();

// Command line arguments take precedence for paths
var inputPath = args.Length > 0 ? args[0] : configuration["InputPath"];
var outputPath = args.Length > 1 ? args[1] : configuration["OutputPath"];

if (string.IsNullOrEmpty(inputPath) || string.IsNullOrEmpty(outputPath))
{
    Console.WriteLine("Usage: Thumbnail.Console <input_path> <output_path>");
    Console.WriteLine("Alternatively, specify InputPath and OutputPath in appsettings.json or environment variables.");
    return;
}

if (!Directory.Exists(outputPath))
{
    Directory.CreateDirectory(outputPath);
}

// Setup DI
var serviceProvider = new ServiceCollection()
    .AddSingleton<IFileSystem, LocalFileSystem>()
    .AddSingleton<IThumbnailGenerator, FfmpegGenerator>()
    .AddSingleton(options)
    .AddSingleton<ThumbnailService>()
    .BuildServiceProvider();

var thumbnailService = serviceProvider.GetRequiredService<ThumbnailService>();

Console.WriteLine($"Starting scan of {inputPath}...");
Console.WriteLine($"Concurrency: {options.MaxThumbnailWorkers} workers");
var stats = await thumbnailService.ProcessDirectoryAsync(inputPath, outputPath);
stats.PrintSummary();
