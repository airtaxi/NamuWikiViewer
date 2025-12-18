using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace NamuWikiViewer.Windows.Messages;

public class AutoSuggestBoxQuerySubmittedMessage(MainWindow window, string queryText) : ValueChangedMessage<string>(queryText)
{
    public MainWindow MainWindow { get; } = window;
}

public class AutoSuggestBoxTextChangedMessage(MainWindow window, string text, AutoSuggestionBoxTextChangeReason reason) : ValueChangedMessage<(string Text, AutoSuggestionBoxTextChangeReason Reason)>((text, reason))
{
    public MainWindow MainWindow { get; } = window;
}

public class AutoSuggestBoxItemsSourceMessage(MainWindow window, IEnumerable<string> items) : ValueChangedMessage<IEnumerable<string>>(items)
{
    public MainWindow MainWindow { get; } = window;
}

public class SetAutoSuggestBoxTextMessage(MainWindow window, string text) : ValueChangedMessage<string>(text)
{
    public MainWindow MainWindow { get; } = window;
}
