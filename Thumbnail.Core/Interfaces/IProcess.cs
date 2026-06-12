namespace Thumbnail.Core.Interfaces;

public interface IProcess : IDisposable
{
    void Kill();
    Task<int> WaitForCompletionAsync();
    string GetStderr();
}
