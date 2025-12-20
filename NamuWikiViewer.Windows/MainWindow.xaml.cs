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
    public bool IsFocused { get; private set; }

    public event EventHandler<object> TitleBarBackRequested;
    public event EventHandler<object> TitleBarPaneToggleRequested;
    public event EventHandler<object> TitleBarHomeRequested;

    private readonly ObservableCollection<string> _autoSuggestionItems = new();

    public MainWindow(string pageToOpen = "나무위키:대문")
    {
        InitializeComponent();

        SetTitleBar(AppTitleBar);
        ExtendsContentIntoTitleBar = true;

        AppWindow.SetIcon("Assets/logo.ico");

        TitleBarAutoSuggestBox.ItemsSource = _autoSuggestionItems;

        WeakReferenceMessenger.Default.Register<AutoSuggestBoxItemsSourceMessage>(this, OnAutoSuggestBoxItemsSourceMessageReceived);
        WeakReferenceMessenger.Default.Register<SetAutoSuggestBoxTextMessage>(this, OnSetAutoSuggestBoxTextMessageReceived);

        MainFrame.Navigate(typeof(MainPage), (this, pageToOpen));
    }

    public void ToggleHomeButton(bool show) => HomeButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    public void ToggleBackButton(bool show) => AppTitleBar.IsBackButtonVisible = show;

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

    private void OnAppTitleBarBackRequested(TitleBar sender, object args) => TitleBarBackRequested?.Invoke(this, args);
    private void OnAppTitleBarPaneToggleRequested(TitleBar sender, object args) => TitleBarPaneToggleRequested?.Invoke(this, args);
    private void OnAppTitleBarHomeClicked(object sender, RoutedEventArgs e) => TitleBarHomeRequested?.Invoke(this, e);

    private void OnRandomPageButtonClicked(object sender, RoutedEventArgs e)
    {
        // Logic to request a random page
        WeakReferenceMessenger.Default.Send(new RandomPageRequestedMessage(this));
    }

    private void OnWindowClosed(object sender, WindowEventArgs args) => BrowserPage.PurgeWebViewCacheForWindow(this);

    private void OnTitleBarAutoSuggestBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args) => WeakReferenceMessenger.Default.Send(new AutoSuggestBoxQuerySubmittedMessage(this, args.QueryText));

    private void OnTitleBarAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

        WeakReferenceMessenger.Default.Send(new AutoSuggestBoxTextChangedMessage(this, sender.Text, args.Reason));
    }

    private void OnTitleBarAutoSuggestBoxPreviewKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == global::Windows.System.VirtualKey.Enter)
        {
            WeakReferenceMessenger.Default.Send(new AutoSuggestBoxQuerySubmittedMessage(this, TitleBarAutoSuggestBox.Text));
            e.Handled = true;
        }
    }

    private void OnAutoSuggestBoxItemsSourceMessageReceived(object recipient, AutoSuggestBoxItemsSourceMessage message)
    {
        if (message.MainWindow != this) return;

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
        if (message.MainWindow != this) return;

        DispatcherQueue.TryEnqueue(() =>
        {
            TitleBarAutoSuggestBox.Text = message.Value;
        });
    }
}
