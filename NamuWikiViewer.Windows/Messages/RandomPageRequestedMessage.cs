using CommunityToolkit.Mvvm.Messaging.Messages;

namespace NamuWikiViewer.Windows.Messages;

public class RandomPageRequestedMessage(MainWindow window) : ValueChangedMessage<bool>(true)
{
    public MainWindow MainWindow { get; } = window;
}
