using CommunityToolkit.Mvvm.Messaging;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NamuWikiViewer.Windows.Controls;
using NamuWikiViewer.Windows.Messages;
using System.Threading.Tasks;
using Windows.System;

namespace NamuWikiViewer.Windows.Pages;

public sealed partial class MainPage : Page
{
    public MainWindow ParentWindow { get; private set; }
    public Frame AppFrame => MainFrame;
    public int BackStackDepth => MainFrame.BackStackDepth;

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

        if (e.Parameter is not (MainWindow mainWindow, string pageName)) return;

        ParentWindow = mainWindow;

        if (e.NavigationMode == NavigationMode.New)
        {
            ParentWindow.TitleBarBackRequested += OnParentTitleBarBackRequested;
            ParentWindow.TitleBarPaneToggleRequested += OnParentTitleBarPaneToggleRequested;
            ParentWindow.TitleBarHomeRequested += OnParentTitleBarHomeRequested;

            MainFrame.Navigate(typeof(BrowserPage), (pageName, Guid.NewGuid().ToString(), this));

            if (pageName != "나무위키:대문") ParentWindow.ToggleHomeButton(true);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (e.NavigationMode == NavigationMode.Back)
        {
            ParentWindow.TitleBarBackRequested -= OnParentTitleBarBackRequested;
            ParentWindow.TitleBarPaneToggleRequested -= OnParentTitleBarPaneToggleRequested;
            ParentWindow.TitleBarHomeRequested -= OnParentTitleBarHomeRequested;
        }
    }

    public void NavigateToPage(string pageName) => MainFrame.Navigate(typeof(BrowserPage), (pageName, Guid.NewGuid().ToString(), this));

    public BrowserPage GetCurrentBrowserPage() => MainFrame.Content as BrowserPage;

    private async Task LaunchPathUriAsync(string path)
    {
        var browserPage = GetCurrentBrowserPage();
        if (browserPage is null) return;


        var pageName = browserPage.PageName;
        await Launcher.LaunchUriAsync(new Uri($"https://namu.wiki/{path}/{pageName}"));
    }

    private void OnParentTitleBarBackRequested(object sender, object e)
    {
        if (!MainFrame.CanGoBack) return;

        MainFrame.GoBack();
    }
    private void OnParentTitleBarPaneToggleRequested(object sender, object e) => ToggleMenu();
    private void OnParentTitleBarHomeRequested(object sender, object e)
    {
        NavigateToPage("나무위키:대문");
        MainFrame.BackStack.Clear();

        BrowserPage.DisposeAllWithoutCurrentPage(ParentWindow);

        ParentWindow.ToggleBackButton(false);
        ParentWindow.ToggleHomeButton(false);
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

        var hasBackstack = frame.BackStackDepth > 0;
        ParentWindow.ToggleBackButton(hasBackstack);
        // ToggleHomeButton should be set at OnNavigatedTo because this method lacks navigation parameter info

        if (frame.Content is BrowserPage)
        {
            ParentWindow.ShowSearchBar();
        }
        else
        {
            ParentWindow.HideSearchBar();
        }
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

    private async void OnOpenNewWindowButtonClicked(object sender, RoutedEventArgs e)
    {
        var element = sender as FrameworkElement;
        if (element.Tag is not string id)
        {
            await MessageBox.ShowAsync(true, ParentWindow, "페이지 ID가 올바르지 않습니다", "오류");
            return;
        }

        var pendingPage = App.GlobalPreferenceViewModel.PendingPages.FirstOrDefault(p => p.Id == id);
        if (pendingPage == null) return; // Deleted

        App.GlobalPreferenceViewModel.PendingPages.Remove(pendingPage);

        var window = new MainWindow(pendingPage.PageName);
        window.Activate();
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

    private async void OnEditPageButtonClicked(object sender, RoutedEventArgs e) => await LaunchPathUriAsync("edit");
    private async void OnDiscussPageButtonClicked(object sender, RoutedEventArgs e) => await LaunchPathUriAsync("discuss");
    private async void OnHistoryPageButtonClicked(object sender, RoutedEventArgs e) => await LaunchPathUriAsync("history");
    private async void OnBacklinkPageButtonClicked(object sender, RoutedEventArgs e) => await LaunchPathUriAsync("backlink");

    private void OnIncreaseFontSizePageButtonClicked(object sender, RoutedEventArgs e)
    {
        var previousFontScale = App.GlobalPreferenceViewModel.FontScale;

        App.GlobalPreferenceViewModel.FontScale = Math.Clamp(App.GlobalPreferenceViewModel.FontScale + 0.1, Constants.MinFontScale, Constants.MaxFontScale);
        if (previousFontScale == App.GlobalPreferenceViewModel.FontScale) return;

        WeakReferenceMessenger.Default.Send(new FontScaleChangedMessage(App.GlobalPreferenceViewModel.FontScale));
    }

    private void OnDecreaseFontSizePageButtonClicked(object sender, RoutedEventArgs e)
    {
        var previousFontScale = App.GlobalPreferenceViewModel.FontScale;

        App.GlobalPreferenceViewModel.FontScale = Math.Clamp(App.GlobalPreferenceViewModel.FontScale - 0.1, Constants.MinFontScale, Constants.MaxFontScale);
        if (previousFontScale == App.GlobalPreferenceViewModel.FontScale) return;

        WeakReferenceMessenger.Default.Send(new FontScaleChangedMessage(App.GlobalPreferenceViewModel.FontScale));
    }

    private void OnResetFontSizePageButtonClicked(object sender, RoutedEventArgs e)
    {
        var previousFontScale = App.GlobalPreferenceViewModel.FontScale;

        App.GlobalPreferenceViewModel.FontScale = 1.0;
        if (previousFontScale == App.GlobalPreferenceViewModel.FontScale) return;

        WeakReferenceMessenger.Default.Send(new FontScaleChangedMessage(App.GlobalPreferenceViewModel.FontScale));
    }
}
