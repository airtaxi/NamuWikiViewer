using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NamuWikiViewer.Windows.Messages;
using NamuWikiViewer.Windows.Pages;
using System.Collections.ObjectModel;
using WinUIEx;
using TitleBar = Microsoft.UI.Xaml.Controls.TitleBar;

namespace NamuWikiViewer.Windows;

public sealed partial class MainWindow : WindowEx
{
    public event EventHandler<object> TitleBarBackRequested;
    public event EventHandler<object> TitleBarPaneToggleRequested;

    private readonly ObservableCollection<string> _autoSuggestionItems = new();

    public MainWindow()
    {
        InitializeComponent();

        SetTitleBar(AppTitleBar);
        ExtendsContentIntoTitleBar = true;

        AppWindow.SetIcon("Assets/logo.ico");

        TitleBarAutoSuggestBox.ItemsSource = _autoSuggestionItems;

        WeakReferenceMessenger.Default.Register<AutoSuggestBoxItemsSourceMessage>(this, OnAutoSuggestBoxItemsSourceMessageReceived);
        WeakReferenceMessenger.Default.Register<SetAutoSuggestBoxTextMessage>(this, OnSetAutoSuggestBoxTextMessageReceived);

        MainFrame.Navigate(typeof(MainPage), this);
    }

    public void ToggleBackButton(bool show) => AppTitleBar.IsBackButtonVisible = show;

    private void OnAppTitleBarBackRequested(TitleBar sender, object args) => TitleBarBackRequested?.Invoke(this, args);
    private void OnAppTitleBarPaneToggleRequested(TitleBar sender, object args) => TitleBarPaneToggleRequested?.Invoke(this, args);

    private void OnWindowClosed(object sender, WindowEventArgs args) => BrowserPage.PurgeWebViewCacheForWindow(this);

    public void ShowLoading(string message = null)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            MainFrame.IsEnabled = false;

            if (string.IsNullOrWhiteSpace(message)) LoadingTextBlock.Visibility = Visibility.Collapsed;
            else
            {
                LoadingTextBlock.Visibility = Visibility.Visible;
                LoadingTextBlock.Text = message;
            }
            LoadingGrid.Visibility = Visibility.Visible;
        });
    }

    public void HideLoading()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            LoadingGrid.Visibility = Visibility.Collapsed;
            MainFrame.IsEnabled = true;
        });
    }

    public void ShowSearchBar()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            TitleBarAutoSuggestBox.Visibility = Visibility.Visible;
        });
    }

    public void HideSearchBar()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            TitleBarAutoSuggestBox.Visibility = Visibility.Collapsed;
        });
    }

    private void OnTitleBarAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        WeakReferenceMessenger.Default.Send(new AutoSuggestBoxQuerySubmittedMessage(args.QueryText));
    }

    private void OnTitleBarAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

        WeakReferenceMessenger.Default.Send(new AutoSuggestBoxTextChangedMessage(sender.Text, args.Reason));
    }

    private void OnTitleBarAutoSuggestBoxPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == global::Windows.System.VirtualKey.Enter)
        {
            WeakReferenceMessenger.Default.Send(new AutoSuggestBoxQuerySubmittedMessage(TitleBarAutoSuggestBox.Text));
            e.Handled = true;
        }
    }

    private void OnAutoSuggestBoxItemsSourceMessageReceived(object recipient, AutoSuggestBoxItemsSourceMessage message)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var newItems = message.Value.ToList();

            for (var i = 0; i < newItems.Count; i++)
            {
                if (i < _autoSuggestionItems.Count)
                {
                    if (_autoSuggestionItems[i] != newItems[i])
                        _autoSuggestionItems[i] = newItems[i];
                }
                else
                {
                    _autoSuggestionItems.Add(newItems[i]);
                }
            }

            while (_autoSuggestionItems.Count > newItems.Count)
            {
                _autoSuggestionItems.RemoveAt(_autoSuggestionItems.Count - 1);
            }
        });
    }

    private void OnSetAutoSuggestBoxTextMessageReceived(object recipient, SetAutoSuggestBoxTextMessage message)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            TitleBarAutoSuggestBox.Text = message.Value;
        });
    }
}
