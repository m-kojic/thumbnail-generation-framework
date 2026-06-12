using System.Diagnostics;
using System.Text;
using Thumbnail.Core.Interfaces;

namespace Thumbnail.Infrastructure;

public class ProcessWrapper : IProcess
{
    private readonly Process _process;
    private readonly StringBuilder _stderr = new();

    public ProcessWrapper(string fileName, string arguments)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        _process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                _stderr.AppendLine(e.Data);
            }
        };
    }

    public void Start()
    {
        _process.Start();
        _process.BeginErrorReadLine();
    }

    public void Kill()
    {
        if (!_process.HasExited)
        {
            _process.Kill();
        }
    }

    public async Task<int> WaitForCompletionAsync()
    {
        await _process.WaitForExitAsync();
        return _process.ExitCode;
    }

    public string GetStderr()
    {
        return _stderr.ToString();
    }

    public void Dispose()
    {
        _process.Dispose();
    }
}
