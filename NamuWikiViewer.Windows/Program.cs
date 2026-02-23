using System.IO.Pipes;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace NamuWikiViewer.Windows;

/// <summary>
/// 단일 인스턴스 실행을 보장하는 커스텀 진입점.
/// Named Mutex로 기존 인스턴스를 감지하고, Named Pipe로 활성화 신호를 전달합니다.
/// </summary>
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
            // 기존 인스턴스에 연결할 수 없는 경우 조용히 종료
        }
    }
}
