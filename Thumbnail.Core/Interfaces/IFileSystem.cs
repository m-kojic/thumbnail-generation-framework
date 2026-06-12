namespace Thumbnail.Core.Interfaces;

public interface IFileSystem
{
    Task<IEnumerable<string>> ListFilesAsync(string path);
    Task<IFileInfo> OpenFileAsync(string path);
    Task<bool> FileExistsAsync(string path);
    Task EnsureDirectoryExistsAsync(string path);
}
