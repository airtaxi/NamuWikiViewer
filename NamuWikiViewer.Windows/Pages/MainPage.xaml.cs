using CommunityToolkit.Mvvm.Messaging;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NamuWikiViewer.Windows.Controls;
using NamuWikiViewer.Windows.Messages;
using System.Threading.Tasks;

namespace NamuWikiViewer.Windows.Pages;

public sealed partial class MainPage : Page
{
    public MainWindow ParentWindow { get; private set; }

    private bool _isMenuOpen = false;
    private bool _isAnimating = false;

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
            ParentWindow.TitleBarPaneToggleRequested += OnParentTitleBarPaneToggleRequested;
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

    private void OnParentTitleBarPaneToggleRequested(object sender, object e)
    {
        ToggleMenu();
    }

    private void ToggleMenu()
    {
        if (_isAnimating)
        {
            OpenMenuStoryboard.Stop();
            CloseMenuStoryboard.Stop();
        }

        _isMenuOpen = !_isMenuOpen;
        _isAnimating = true;

        if (_isMenuOpen)
        {
            MenuBackdrop.Visibility = Visibility.Visible;
            MenuGrid.Visibility = Visibility.Visible;
            OpenMenuStoryboard.Begin();
        }
        else
        {
            CloseMenuStoryboard.Begin();
        }
    }

    private void OnCloseMenuStoryboardCompleted(object sender, object e)
    {
        MenuBackdrop.Visibility = Visibility.Collapsed;
        MenuGrid.Visibility = Visibility.Collapsed;
        _isAnimating = false;
    }

    private void OnOpenMenuStoryboardCompleted(object sender, object e)
    {
        _isAnimating = false;
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
            await MessageBox.ShowAsync(true, ParentWindow, "페이지 ID가 올바르지 않습니다", "오류");
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
            await MessageBox.ShowAsync(true, ParentWindow,"페이지 ID가 올바르지 않습니다", "오류");
            return;
        }

        var pendingPage = App.GlobalPreferenceViewModel.PendingPages.FirstOrDefault(p => p.Id == id);
        if (pendingPage == null) return; // Deleted 

        NavigateToPage(pendingPage.PageName);
        App.GlobalPreferenceViewModel.PendingPages.Remove(pendingPage);
    }

    private async void OnPendingPageAddedMessageReceived(object _, PendingPageAddedMessage __)
    {
        await Task.Delay(100);
        PendingPageScrollViewer.ChangeView(PendingPageScrollViewer.ScrollableWidth, null, null);
    }

    private async void OnSettingsButtonClicked(object sender, RoutedEventArgs e)
    {
        var settingsDialog = new SettingsDialog { XamlRoot = XamlRoot };
        await settingsDialog.ShowAsync();
    }

    private void OnMenuBackdropTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (_isMenuOpen)
        {
            ToggleMenu();
        }
    }

    private void OnPendingPageScrollViewerPointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(PendingPageScrollViewer);
        var delta = pointer.Properties.MouseWheelDelta;
        
        var currentOffset = PendingPageScrollViewer.HorizontalOffset;
        var newOffset = currentOffset - delta;
        
        PendingPageScrollViewer.ChangeView(newOffset, null, null, false);
        
        e.Handled = true;
    }

    private async void OnHistoryButtonClicked(object sender, RoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        var id = element.Tag as string;

        var pageHistory = App.GlobalPreferenceViewModel.PageHistories.FirstOrDefault(p => p.Id == id);
        if (pageHistory == null)
        {
            await MessageBox.ShowAsync(true, ParentWindow, "페이지 기록이 존재하지 않습니다", "오류");
            return;
        }

        NavigateToPage(pageHistory.PageName);
        App.GlobalPreferenceViewModel.PageHistories.Remove(pageHistory);
    }

    private async void OnDeleteHistoryButtonClicked(object sender, RoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        var id = element.Tag as string;

        var pageHistory = App.GlobalPreferenceViewModel.PageHistories.FirstOrDefault(p => p.Id == id);
        if (pageHistory == null)
        {
            await MessageBox.ShowAsync(true, ParentWindow, "페이지 기록이 존재하지 않습니다", "오류");
            return;
        }

        App.GlobalPreferenceViewModel.PageHistories.Remove(pageHistory);
    }
}
