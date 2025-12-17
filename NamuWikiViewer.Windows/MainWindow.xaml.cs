using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NamuWikiViewer.Windows.Pages;
using WinUIEx;
using TitleBar = Microsoft.UI.Xaml.Controls.TitleBar;

namespace NamuWikiViewer.Windows;

public sealed partial class MainWindow : WindowEx
{
    public event EventHandler<object> TitleBarBackRequested;
    public event EventHandler<object> TitleBarPaneToggleRequested;

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
    private void OnAppTitleBarPaneToggleRequested(TitleBar sender, object args) => TitleBarPaneToggleRequested?.Invoke(this, args);

    private void OnWindowClosed(object sender, WindowEventArgs args) => BrowserPage.PurgeWebViewCacheForWindow(this);

}
