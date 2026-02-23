using System.IO.Pipes;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace NamuWikiViewer.Windows;

static class Program
{
    internal const string MutexName = "NamuWikiViewer_SingleInstance_Mutex";
    internal const string PipeName = "NamuWikiViewer_SingleInstance_Pipe";

    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirstInstance);

        if (!isFirstInstance)
        {
            NotifyExistingInstance();
            return;
        }

        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }

    private static void NotifyExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(3000);
            using var writer = new StreamWriter(client);
            writer.Write("ACTIVATE");
            writer.Flush();
        }
        catch
        {
            // Will be closed silently if the existing instance is not responding or pipe connection fails.
        }
    }
}
