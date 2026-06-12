namespace Thumbnail.Core.Interfaces;

public interface IFileInfo
{
    string Name { get; }
    long Size { get; }
    bool IsDirectory { get; }
}
