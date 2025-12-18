using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace NamuWikiViewer.Windows.Messages;

public class AutoSuggestBoxQuerySubmittedMessage(string queryText) : ValueChangedMessage<string>(queryText);

public class AutoSuggestBoxTextChangedMessage(string text, AutoSuggestionBoxTextChangeReason reason) : ValueChangedMessage<(string Text, AutoSuggestionBoxTextChangeReason Reason)>((text, reason));

public class AutoSuggestBoxItemsSourceMessage(IEnumerable<string> items) : ValueChangedMessage<IEnumerable<string>>(items);

public class SetAutoSuggestBoxTextMessage(string text) : ValueChangedMessage<string>(text);
