using Thumbnail.Core.Interfaces;

namespace Thumbnail.Infrastructure;

public class LocalFileInfo : IFileInfo
{
    private readonly FileInfo _fileInfo;

    public LocalFileInfo(string path)
    {
        _fileInfo = new FileInfo(path);
    }

    public string Name => _fileInfo.Name;
    public long Size => _fileInfo.Length;
    public bool IsDirectory => (_fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
}

public class LocalFileSystem : IFileSystem
{
    public Task<IEnumerable<string>> ListFilesAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
        return Task.FromResult((IEnumerable<string>)files);
    }

    public Task<IFileInfo> OpenFileAsync(string path)
    {
        return Task.FromResult((IFileInfo)new LocalFileInfo(path));
    }

    public Task<bool> FileExistsAsync(string path)
    {
        return Task.FromResult(File.Exists(path));
    }

    public Task EnsureDirectoryExistsAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return Task.CompletedTask;
    }
}
