using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NamuWikiViewer.Windows.Pages;

namespace NamuWikiViewer.Windows;

public sealed partial class MainWindow : Window
{
    public event EventHandler<object> TitleBarBackRequested;

    public MainWindow()
    {
        InitializeComponent();

        SetTitleBar(AppTitleBar);
        ExtendsContentIntoTitleBar = true;

        AppWindow.SetIcon("Assets/logo.ico");

        MainFrame.Navigate(typeof(MainPage), this);
    }

    public void ToggleBackButton(bool show) => AppTitleBar.IsBackButtonVisible = show;

    private void OnAppTitleBarBackRequested(TitleBar sender, object args) => TitleBarBackRequested?.Invoke(this, args);

    private void OnWindowClosed(object sender, WindowEventArgs args) => BrowserPage.PurgeWebViewCacheForWindow(this);
}
