using System.IO.Pipes;
using Microsoft.UI.Xaml;
using NamuWikiViewer.Windows.ViewModels;

namespace NamuWikiViewer.Windows;

public partial class App : Application
{
    private Window _window;
    private readonly CancellationTokenSource _pipeListenerCancellationTokenSource = new();

    public static PreferenceViewModel GlobalPreferenceViewModel { get; } = new();

    public App()
    {
        InitializeComponent();
        _ = ListenForNewInstancesAsync(_pipeListenerCancellationTokenSource.Token);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    /// <summary>
    /// Named Pipe 서버를 통해 새 인스턴스의 활성화 요청을 수신합니다.
    /// 요청 수신 시 UI 스레드에서 새 MainWindow를 생성하고 활성화합니다.
    /// </summary>
    private async Task ListenForNewInstancesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    Program.PipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(cancellationToken);

                using var reader = new StreamReader(server);
                var message = await reader.ReadToEndAsync(cancellationToken);

                if (message == "ACTIVATE")
                {
                    _window.DispatcherQueue.TryEnqueue(() =>
                    {
                        var newWindow = new MainWindow();
                        newWindow.Activate();
                    });
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
