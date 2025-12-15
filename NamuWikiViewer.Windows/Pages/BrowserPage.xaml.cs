using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using NamuWikiViewer.Commons.Models;
using NamuWikiViewer.Windows.Messages;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;
using WinRT;

namespace NamuWikiViewer.Windows.Pages;

public sealed partial class BrowserPage : Page
{
    private const string BaseUrl = "https://namu.wiki/w/";

    // For caching WebView2 instances (to preserve state)
    private static readonly Dictionary<string, WebView2> WebViewCache = [];

    // To track WebView2 instances per MainWindow (to prevent memory leaks)
    private static readonly Dictionary<MainWindow, List<string>> MainWindowWebViewKeys = [];

    public static void PurgeWebViewCacheForWindow(MainWindow window)
    {
        if (MainWindowWebViewKeys.TryGetValue(window, out var keys))
        {
            foreach (var key in keys)
            {
                if (WebViewCache.TryGetValue(key, out var webView))
                {
                    WebViewCache.Remove(key);
                }
            }

            MainWindowWebViewKeys.Remove(window);
        }
    }

    private bool _isFirstNavigation;
    private string _pageName;
    private string _pageHash;
    private MainPage _parent;
    private WebView2 _mainWebView;

    private string _pendingPageName;
    private CancellationTokenSource _pendingPageNameCts;

    public BrowserPage()
    {
        InitializeComponent();
        _pageName = "나무위키:대문";

        WeakReferenceMessenger.Default.Register<ValueChangedMessage<Preference>>(this, OnPreferenceChanged);
    }

    private async void OnPreferenceChanged(object recipient, ValueChangedMessage<Preference> message)
    {
        var newPreference = message.Value;
        await UpdateScrollBarVisibilityAsync(newPreference);
        await UpdateAdsVisibility(newPreference);
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is MainPage mainPage) _parent = mainPage;
        else if (e.Parameter is (string pageName, string pageHash, MainPage mainPage2))
        {
            _pageName = pageName;
            _pageHash = pageHash;
            _parent = mainPage2;
        }

        if (WebViewCache.TryGetValue(_pageName + _pageHash, out var cachedWebView))
        {
            _mainWebView = cachedWebView;

            if (_mainWebView.Parent is Grid parentGrid)
            {
                parentGrid.Children.Remove(_mainWebView);
            }

            WebViewContainer.Content = _mainWebView;
        }
        else
        {
            _isFirstNavigation = true;

            _mainWebView = new WebView2();
            WebViewContainer.Content = _mainWebView;
            WebViewCache[_pageName + _pageHash] = _mainWebView;

            // Track WebView keys for the parent MainWindow (to prevent memory leaks)
            if (!MainWindowWebViewKeys.ContainsKey(_parent.ParentWindow)) MainWindowWebViewKeys[_parent.ParentWindow] = [];
            MainWindowWebViewKeys[_parent.ParentWindow].Add(_pageName + _pageHash);

            await _mainWebView.EnsureCoreWebView2Async();

            _mainWebView.Source = new Uri(BaseUrl + _pageName);
            await UpdateScrollBarVisibilityAsync(App.GlobalPreferenceViewModel.Preference);
        }

        _mainWebView.CoreWebView2.DOMContentLoaded += OnDOMContentLoaded;
        _mainWebView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
        _mainWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        _mainWebView.CoreWebView2.NavigationStarting += OnNavigationStarting;
        _mainWebView.CoreWebView2.ContextMenuRequested += OnContextMenuRequested;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (_mainWebView != null)
        {
            _mainWebView.CoreWebView2.DOMContentLoaded -= OnDOMContentLoaded;
            _mainWebView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
            _mainWebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            _mainWebView.CoreWebView2.NavigationStarting -= OnNavigationStarting;
            _mainWebView.CoreWebView2.ContextMenuRequested -= OnContextMenuRequested;
            WebViewContainer.Content = null;
        }

        if (e.NavigationMode == NavigationMode.Back) WebViewCache.Remove(_pageName + _pageHash);
    }

    private async void OnContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
    {
        args.Handled = true;

        if (args.ContextMenuTarget.LinkUri?.StartsWith(BaseUrl) == true)
        {
            var pageName = HttpUtility.UrlDecode(args.ContextMenuTarget.LinkUri[BaseUrl.Length..]);

            _pendingPageName = pageName;

            PendingPageNameTextBlock.Text = pageName;
            PendingPageNameGrid.Visibility = Visibility.Visible;

            _pendingPageNameCts?.Cancel();
            _pendingPageNameCts = new CancellationTokenSource();
            try { await Task.Delay(3000, _pendingPageNameCts.Token); }
            catch (TaskCanceledException) { return; }

            PendingPageNameGrid.Visibility = Visibility.Collapsed;
        }
    }

    private async void OnDOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
    {
        await HideHeaderAsync();
        await UpdateScrollBarVisibilityAsync(App.GlobalPreferenceViewModel.Preference);
        await UpdateAdsVisibility(App.GlobalPreferenceViewModel.Preference);
        await InjectNavigationInterceptScriptAsync();
    }

    private async Task HideHeaderAsync()
    {
        var script = @"
            (function() {
                var style = document.createElement('style');
                style.type = 'text/css';
                style.innerHTML = '#app > div:first-child > div:first-child { display: none !important; } #app > div:first-child > div:nth-child(2) > div > div:nth-child(4) > article > div:nth-child(2) { display: none !important; } #app > div:first-child > div:nth-child(2) > div > div:nth-child(4) > div:nth-child(2) > div:nth-child(2) { display: none !important; }';
                document.head.appendChild(style);
                
                var xpath1 = '/div[1]/div[1]';
                var element1 = document.evaluate(xpath1, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                if (element1) {
                    element1.style.display = 'none';
                }
                
                var xpath2 = '/div[1]/div[2]/div/div[4]/article/div[2]';
                var element2 = document.evaluate(xpath2, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                if (element2) {
                    element2.style.display = 'none';
                } else {
                    var xpath3 = '/div[1]/div[2]/div/div[4]/div[2]/div[2]';
                    var element3 = document.evaluate(xpath3, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                    if (element3) {
                        element3.style.display = 'none';
                    }
                }
            })();
        ";

        await _mainWebView.ExecuteScriptAsync(script);
    }

    private async Task UpdateAdsVisibility(Preference preference)
    {
        try
        {
            if (_mainWebView?.CoreWebView2 == null) return;

            if (preference.BlockAds)
            {
                var script = @"
                    (function() {
                        var style = document.getElementById('hideAdsStyle');
                        if (!style) {
                            style = document.createElement('style');
                            style.id = 'hideAdsStyle';
                            style.type = 'text/css';
                            style.innerHTML = '#app > div:first-child > div:nth-child(2) > div > div:nth-child(4) > article > div:nth-child(3) { display: none !important; } #app > div:first-child > div:nth-child(2) > div > div:nth-child(4) > div:nth-child(2) > div:nth-child(3) { display: none !important; }';
                            document.head.appendChild(style);
                        }
                        
                        var xpath1 = '/div[1]/div[2]/div/div[4]/article/div[3]';
                        var element1 = document.evaluate(xpath1, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                        if (element1) {
                            element1.style.display = 'none';
                        } else {
                            var xpath2 = '/div[1]/div[2]/div/div[4]/div[2]/div[3]';
                            var element2 = document.evaluate(xpath2, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                            if (element2) {
                                element2.style.display = 'none';
                            }
                        }
                    })();
                ";
                await _mainWebView.ExecuteScriptAsync(script);
            }
            else
            {
                var script = @"
                    (function() {
                        var style = document.getElementById('hideAdsStyle');
                        if (style) {
                            style.parentNode.removeChild(style);
                        }
                        
                        var xpath1 = '/div[1]/div[2]/div/div[4]/article/div[3]';
                        var element1 = document.evaluate(xpath1, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                        if (element1) {
                            element1.style.display = '';
                        } else {
                            var xpath2 = '/div[1]/div[2]/div/div[4]/div[2]/div[3]';
                            var element2 = document.evaluate(xpath2, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
                            if (element2) {
                                element2.style.display = '';
                            }
                        }
                    })();
                ";
                await _mainWebView.ExecuteScriptAsync(script);
            }
        }
        catch (ObjectDisposedException) { } // WebView might be disposed
    }

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var url = args.TryGetWebMessageAsString();
        
        if (url.StartsWith(BaseUrl))
        {
            var pageName = HttpUtility.UrlDecode(url[BaseUrl.Length..]);
            _parent.NavigateToPage(pageName);
        }
    }

    private void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        var url = args.Uri;

        if (_isFirstNavigation)
        {
            _isFirstNavigation = false;
            return;
        }

        args.Cancel = true;

        if (url.StartsWith(BaseUrl))
        {
            var pageName = HttpUtility.UrlDecode(url[BaseUrl.Length..]);
            _parent.NavigateToPage(pageName);
        }
    }

    private void OnNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args) => args.Handled = true;

    private async void OnAddPendingPageButtonClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PendingPageNameGrid.Visibility = Visibility.Collapsed;

        var pageName = _pendingPageName;
        if (string.IsNullOrEmpty(pageName))
        {
            await MessageBox.ShowErrorAsync(true, "잘못된 페이지 이름입니다.", "오류");
            return;
        }

        var alreadyExists = App.GlobalPreferenceViewModel.PendingPages.Any(p => p.PageName == pageName);
        if (alreadyExists)
        {
            var result = await MessageBox.ShowAsync(true, "이미 추가된 페이지입니다. 그래도 추가하시겠습니까?", "경고", MessageBoxButtons.YesNo);
            if (result == MessageBoxResult.NO) return;
        }

        var pendingPage = new PendingPage(pageName);
        App.GlobalPreferenceViewModel.PendingPages.Add(pendingPage);

        WeakReferenceMessenger.Default.Send(new PendingPageAddedMessage(pendingPage));
    }

    private async Task InjectNavigationInterceptScriptAsync()
    {
        var script = @"
            (function() {
                // Prevent location changes
                var originalPushState = history.pushState;
                var originalReplaceState = history.replaceState;
                
                history.pushState = function() {
                    originalPushState.apply(history, arguments);
                };
                
                history.replaceState = function() {
                    originalReplaceState.apply(history, arguments);
                };
                
                // Intercept all link clicks
                document.addEventListener('click', function(e) {
                    var target = e.target;
                    while (target && target.tagName !== 'A') {
                        target = target.parentElement;
                    }
                    
                    if (target && target.href) {
                        e.preventDefault();
                        e.stopPropagation();
                        window.chrome.webview.postMessage(target.href);
                        return false;
                    }
                }, true);
                
                // Override window.location setter
                var originalLocation = window.location;
                Object.defineProperty(window, 'location', {
                    get: function() { return originalLocation; },
                    set: function(url) {
                        if (url.startsWith('" + BaseUrl + @"')) {
                            window.chrome.webview.postMessage(url);
                        }
                    }
                });
            })();
        ";

        await _mainWebView.ExecuteScriptAsync(script);
    }

    private async Task UpdateScrollBarVisibilityAsync(Preference newPreference)
    {
        try
        {
            if (_mainWebView?.CoreWebView2 == null) return;

            if (newPreference.HideWebViewScrollBar) await _mainWebView.ExecuteScriptAsync("document.querySelector('body').style.overflow='scroll';var style=document.createElement('style');style.type='text/css';style.innerHTML='::-webkit-scrollbar{display:none}';document.getElementsByTagName('body')[0].appendChild(style)");
            else await _mainWebView.ExecuteScriptAsync("document.querySelector('body').style.overflow='auto';var styles=document.getElementsByTagName('style');for(var i=0;i<styles.length;i++){if(styles[i].innerHTML==='::-webkit-scrollbar{display:none}'){styles[i].parentNode.removeChild(styles[i]);}}");
        }
        catch (ObjectDisposedException) { } // WebView might be disposed
    }
}
