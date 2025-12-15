using CommunityToolkit.Mvvm.Messaging;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NamuWikiViewer.Windows.Messages;

namespace NamuWikiViewer.Windows.Pages;

public sealed partial class MainPage : Page
{
    public MainWindow ParentWindow { get; private set; }

    public MainPage()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<PendingPageAddedMessage>(this, OnPendingPageAddedMessageReceived);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ParentWindow = e.Parameter as MainWindow;

        if (e.NavigationMode == NavigationMode.New)
        {
            ParentWindow.TitleBarBackRequested += OnParentTitleBarBackRequested;
            MainFrame.Navigate(typeof(BrowserPage), this);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (e.NavigationMode == NavigationMode.Back)
        {
            ParentWindow.TitleBarBackRequested -= OnParentTitleBarBackRequested;
        }
    }

    public void NavigateToPage(string pageName) => MainFrame.Navigate(typeof(BrowserPage), (pageName, Guid.NewGuid().ToString(), this));

    private void OnParentTitleBarBackRequested(object sender, object e)
    {
        if (MainFrame.CanGoBack)
        {
            MainFrame.GoBack();
        }
    }

    private void OnMainFrameNavigated(object sender, NavigationEventArgs e)
    {
        var frame = sender as Frame;

        if (frame.BackStackDepth > 0) ParentWindow.ToggleBackButton(true);
        else ParentWindow.ToggleBackButton(false);
    }

    private async void OnDeletePendingPageButtonClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element.Tag is not string id)
        {
            await MessageBox.ShowAsync(true, "페이지 ID가 올바르지 않습니다", "오류");
            return;
        }

        var pendingPage = App.GlobalPreferenceViewModel.PendingPages.FirstOrDefault(p => p.Id == id);
        App.GlobalPreferenceViewModel.PendingPages.Remove(pendingPage);
    }

    private async void OnPendingPageItemTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element.Tag is not string id)
        {
            await MessageBox.ShowAsync(true, "페이지 ID가 올바르지 않습니다", "오류");
            return;
        }

        var pendingPage = App.GlobalPreferenceViewModel.PendingPages.FirstOrDefault(p => p.Id == id);
        if (pendingPage == null) return; // Deleted 

        NavigateToPage(pendingPage.PageName);
        App.GlobalPreferenceViewModel.PendingPages.Remove(pendingPage);
    }

    private void OnPendingPageAddedMessageReceived(object _, PendingPageAddedMessage __) => PendingPageScrollViewer.ChangeView(PendingPageScrollViewer.ScrollableWidth, null, null);
}
