using ABI.Windows.ApplicationModel.Activation;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using NamuWikiViewer.Commons.Models;
using NamuWikiViewer.Windows.Extensions;
using NamuWikiViewer.Windows.Messages;
using RestSharp;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Web;
using Windows.System;
using WinRT;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NamuWikiViewer.Windows.Pages;

public sealed partial class BrowserPage : Page
{
    // For caching WebView2 instances (to preserve state)
    private static readonly Dictionary<string, BrowserPage> PageCache = [];

    // To track WebView2 instances per MainWindow (to prevent memory leaks)
    private static readonly Dictionary<MainWindow, List<string>> MainWindowWebViewKeys = [];

    private static readonly RestClient s_client = new("https://namu.wiki/");

    private static void RemoveCache(MainWindow mainWindow, string key)
    {
        if (PageCache.TryGetValue(key, out var page))
        {
            DisposeCaches(mainWindow, [new(key, page)]);
        }
        else if (MainWindowWebViewKeys.TryGetValue(mainWindow, out var keys))
        {
            keys.Remove(key);
        }
    }

    private static void TrimCacheForWindow(MainWindow mainWindow, int limit, string currentKey)
    {
        if (!MainWindowWebViewKeys.TryGetValue(mainWindow, out var keys)) return;

        while (keys.Count > limit)
        {
            var key = keys.FirstOrDefault(k => k != currentKey) ?? keys.First();
            if (key == currentKey && keys.Count <= 1) break;

            RemoveCache(mainWindow, key);
        }
    }

    public static void PurgeWebViewCacheForWindow(MainWindow window)
    {
        if (MainWindowWebViewKeys.TryGetValue(window, out var keys))
        {
            var pages = PageCache.Where(x => keys.Contains(x.Key)).ToList();
            DisposeCaches(window, pages);
        }
    }

    public static void DisposeAllWithoutCurrentPage(MainWindow mainWindow)
    {
        var keys = MainWindowWebViewKeys[mainWindow];
        var pages = PageCache.Where(x => keys.Contains(x.Key)).ToList();
        if (pages.Count == 0) return;

        var firstPage = pages.FirstOrDefault();
        var mainPage = firstPage.Value._parent;

        var currentPage = mainPage.GetCurrentBrowserPage();

        var pagesWithoutCurrent = pages.Where(x => x.Value != currentPage);

        DisposeCaches(mainWindow, pagesWithoutCurrent);
    }

    private static void DisposeCaches(MainWindow mainWindow, IEnumerable<KeyValuePair<string, BrowserPage>> pairs)
    {
        foreach (var pair in pairs.ToList())
        {
            var page = pair.Value;
            var webView = page?.WebView;
            if (page != null && webView != null)
            {
                webView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
                webView.CoreWebView2.DOMContentLoaded -= page.OnDOMContentLoaded;
                webView.CoreWebView2.WebMessageReceived -= page.OnWebMessageReceived;
                webView.CoreWebView2.NavigationStarting -= page.OnNavigationStarting;
                webView.CoreWebView2.ContextMenuRequested -= page.OnContextMenuRequested;

                webView.Close();
            }

            PageCache.Remove(pair.Key);

            MainWindowWebViewKeys.TryGetValue(mainWindow, out var keys);
            keys?.Remove(pair.Key);
        }
    }

    public string PageName { get; private set; }
    public WebView2 WebView { get; private set; }

    private bool IsActive => (Page)_parent.AppFrame.Content == this;

    private bool _isFirstNavigation;
    private string _pageHash;
    private MainPage _parent;

    private string _pendingPageName;
    private CancellationTokenSource _pendingPageNameCts;
    private CancellationTokenSource _autoSuggestCts;
    private string _lastQueryText;

    public BrowserPage()
    {
        InitializeComponent();
        PageName = "나무위키:대문";

        WeakReferenceMessenger.Default.Register<ValueChangedMessage<Preference>>(this, OnPreferenceChanged);
        WeakReferenceMessenger.Default.Register<FontScaleChangedMessage>(this, OnFontScaleChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<AutoSuggestBoxQuerySubmittedMessage>(this, OnAutoSuggestBoxQuerySubmittedMessageReceived);
        WeakReferenceMessenger.Default.Register<AutoSuggestBoxTextChangedMessage>(this, OnAutoSuggestBoxTextChangedMessageReceived);
        WeakReferenceMessenger.Default.Register<RandomPageRequestedMessage>(this, OnRandomPageRequestedMessageReceived);
    }

    private void TrimBackStack(string cacheKey)
    {
        var limit = App.GlobalPreferenceViewModel.Preference.BackStackDepthLimit;
        if (limit is null) return;

        var frame = _parent.AppFrame;
        while (frame.BackStackDepth > limit)
        {
            var entry = frame.BackStack.FirstOrDefault();
            frame.BackStack.RemoveAt(0);

            if (entry?.Parameter is (string pageName, string pageHash, MainPage mainPage))
            {
                RemoveCache(mainPage.ParentWindow, pageName + pageHash);
            }
        }

        if (!App.GlobalPreferenceViewModel.Preference.DisableWebViewCache)
        {
            TrimCacheForWindow(_parent.ParentWindow, limit.Value, cacheKey);
        }
    }

    public void ShowBrowserSearch()
    {
        if (WebView?.CoreWebView2 == null) return;

        WebView.ShowFindDialog();
    }

    public void HideBrowserSearch()
    {
        if (WebView?.CoreWebView2 == null) return;

        WebView.HideFindDialog();
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
                        // Check if it's a hash link within the same page
                        var currentUrl = window.location.href.split('#')[0];
                        var targetUrl = target.href.split('#')[0];
                        
                        // If it's a hash navigation on the same page, allow default behavior
                        if (currentUrl === targetUrl && target.href.includes('#')) {
                            return true;
                        }
                        
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
                        if (url.startsWith('" + Constants.BaseUrl + @"')) {
                            window.chrome.webview.postMessage(url);
                        }
                    }
                });
            })();
        ";

        await WebView.ExecuteScriptAsync(script);
    }

    private async Task UpdateScrollBarVisibilityAsync(Preference newPreference)
    {
        try
        {
            if (WebView?.CoreWebView2 == null) return;

            if (newPreference.HideWebViewScrollBar) await WebView.ExecuteScriptAsync("document.querySelector('body').style.overflow='scroll';var style=document.createElement('style');style.type='text/css';style.innerHTML='::-webkit-scrollbar{display:none}';document.getElementsByTagName('body')[0].appendChild(style)");
            else await WebView.ExecuteScriptAsync("document.querySelector('body').style.overflow='auto';var styles=document.getElementsByTagName('style');for(var i=0;i<styles.length;i++){if(styles[i].innerHTML==='::-webkit-scrollbar{display:none}'){styles[i].parentNode.removeChild(styles[i]);}}");
        }
        catch (ObjectDisposedException) { } // WebView might be disposed
    }

    private async Task UpdateFontScaleAsync(Preference preference)
    {
        if (WebView?.CoreWebView2 == null) return;

        var script = $$"""
            (function() {
                document.body.style.zoom = '{{preference.FontScale}}';
            })();
        """;
        try { await WebView.ExecuteScriptAsync(script); }
        catch (ObjectDisposedException) { } // WebView might be disposed
    }

    private async Task ProcessUrlAsync(string url)
    {
        if (url.StartsWith(Constants.BaseUrl))
        {
            var pageName = HttpUtility.UrlDecode(url[Constants.BaseUrl.Length..]);
            
            var currentBasePage = PageName?.Split('#')[0];
            var targetBasePage = pageName.Split('#')[0];

            if (currentBasePage == targetBasePage && pageName.Contains('#')) return;

            _parent.NavigateToPage(pageName);
        }
        else if (url.StartsWith("https://namu.wiki/Go?q="))
        {
            var query = HttpUtility.ParseQueryString(new Uri(url).Query).Get("q");
            if (!string.IsNullOrEmpty(query))
            {
                var decodedQuery = HttpUtility.UrlDecode(query);
                _parent.NavigateToPage(decodedQuery);
            }
            else await Launcher.LaunchUriAsync(new Uri(url));
        }
        else await Launcher.LaunchUriAsync(new Uri(url));
    }

    private async Task ValidateAndNavigateAsync(string pageName)
    {
        _lastQueryText = null;
        _autoSuggestCts?.Cancel();
        WeakReferenceMessenger.Default.Send(new AutoSuggestBoxItemsSourceMessage(_parent.ParentWindow, []));

        _parent.ParentWindow.ShowLoading();
        try
        {
            var request = new RestRequest($"/w/{pageName}", Method.Get);

            var response = await s_client.ExecuteAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.OK) _parent.NavigateToPage(pageName);
            else await MessageBox.ShowErrorAsync(true, _parent.ParentWindow, "해당 페이지를 찾을 수 없습니다.", "오류");
        }
        finally { _parent.ParentWindow.HideLoading(); }
    }

    private async Task UpdateTimestampAsync()
    {
        var script = @"
            (function() {
                var timeElement = document.querySelector('time');
                if (!timeElement) return;
                
                var datetime = timeElement.getAttribute('datetime');
                if (!datetime) return;
                
                var date = new Date(datetime);
                var year = date.getFullYear();
                var month = String(date.getMonth() + 1).padStart(2, '0');
                var day = String(date.getDate()).padStart(2, '0');
                var hours = String(date.getHours()).padStart(2, '0');
                var minutes = String(date.getMinutes()).padStart(2, '0');
                var seconds = String(date.getSeconds()).padStart(2, '0');
                
                var formattedDate = `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
                var text = '최근 수정 시각: ' + formattedDate;
                
                var existingDiv = document.getElementById('customTimestampDiv');
                if (existingDiv) {
                    existingDiv.innerText = text;
                    return;
                }
                
                var div = document.createElement('div');
                div.id = 'customTimestampDiv';
                div.innerText = text;
                div.style.margin = '4px';
                div.style.fontSize = '12px';
                div.style.color = '#808080';
                
                var app = document.getElementById('app');
                if (app) {
                    app.insertBefore(div, app.firstChild);
                } else {
                    document.body.insertBefore(div, document.body.firstChild);
                }
            })();
        ";

        await WebView.ExecuteScriptAsync(script);
    }

    private async Task HideHeaderAsync()
    {
        var script = @"
            (function() {
                var style = document.createElement('style');
                style.type = 'text/css';
                style.id = 'hideHeaderStyle';
                document.head.appendChild(style);
                
                var h1 = document.querySelector('h1');
                if (h1 && h1.parentElement && h1.parentElement.parentElement) {
                    var headerDiv = h1.parentElement.parentElement;
                    headerDiv.style.display = 'none';
                    style.innerHTML += '#' + (headerDiv.id || '') + ' { display: none !important; }';
                    
                    // Hide additional header component (first child div of first child div of app)
                    var appDiv = document.getElementById('app');
                    if (appDiv && appDiv.firstElementChild && appDiv.firstElementChild.firstElementChild) {
                        var additionalHeaderDiv = appDiv.firstElementChild.firstElementChild;
                        additionalHeaderDiv.style.display = 'none';
                        
                        // Function to adjust margin based on viewport width
                        var contentDiv = additionalHeaderDiv.nextElementSibling;
                        if (contentDiv) {
                            function adjustMargin() {
                                if (window.innerWidth < 1024) {
                                    contentDiv.style.marginTop = '-40px';
                                } else {
                                    contentDiv.style.marginTop = '';
                                }
                            }
                            
                            // Initial adjustment
                            adjustMargin();
                            
                            // Add resize listener
                            window.addEventListener('resize', adjustMargin);
                        }
                    }
                }
            })();
        ";

        await WebView.ExecuteScriptAsync(script);
    }

    private async Task UpdateAdsVisibilityAsync(Preference preference)
    {
        try
        {
            if (WebView?.CoreWebView2 == null) return;

            if (preference.BlockAds)
            {
                var script = @"
                    (function() {
                        var style = document.getElementById('hideAdsStyle');
                        if (!style) {
                            style = document.createElement('style');
                            style.id = 'hideAdsStyle';
                            style.type = 'text/css';
                            document.head.appendChild(style);
                        }
                        
                        // Hide divs with data-google-query-id attribute (Google ads)
                        style.innerHTML = '[data-google-query-id] { display: none !important; }';
                        
                        // Apply display:none directly to existing ad elements
                        var adDivs = document.querySelectorAll('[data-google-query-id]');
                        adDivs.forEach(function(div) {
                            div.style.display = 'none';
                        });
                    })();
                ";
                await WebView.ExecuteScriptAsync(script);
            }
            else
            {
                var script = @"
                    (function() {
                        var style = document.getElementById('hideAdsStyle');
                        if (style) {
                            style.parentNode.removeChild(style);
                        }
                        
                        // Restore display for ad elements
                        var adDivs = document.querySelectorAll('[data-google-query-id]');
                        adDivs.forEach(function(div) {
                            div.style.display = '';
                        });
                    })();
                ";
                await WebView.ExecuteScriptAsync(script);
            }
        }
        catch (ObjectDisposedException) { } // WebView might be disposed
    }

    private async Task QueryAutoSuggestionAsync(string queryText)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            WeakReferenceMessenger.Default.Send(new AutoSuggestBoxItemsSourceMessage(_parent.ParentWindow, []));
            return;
        }

        try
        {
            var escapedQuery = HttpUtility.JavaScriptStringEncode(queryText);
            var script = $$"""
                (async function() {
                    var sendResults = (results) => {
                        var message = {
                            type: 'suggestion',
                            query: '{{escapedQuery}}',
                            items: results
                        };
                        window.chrome.webview.postMessage(JSON.stringify(message));
                    };

                    var input = document.querySelector('input[type="search"]');
                    if (!input) { sendResults([]); return; }

                    var nativeSetter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set;
                    nativeSetter.call(input, '{{escapedQuery}}');

                    input.dispatchEvent(new InputEvent('input', {
                        bubbles: true,
                        inputType: 'insertText',
                        data: '{{escapedQuery}}',
                    }));

                    var form = input.parentElement;
                    if (!form) { sendResults([]); return; }
                    
                    var suggestionsDiv = form.nextElementSibling;
                    if (suggestionsDiv) suggestionsDiv = suggestionsDiv.nextElementSibling;
                    if (suggestionsDiv) suggestionsDiv = suggestionsDiv.nextElementSibling;

                    if (!suggestionsDiv) { sendResults([]); return; }

                    var observer = new MutationObserver((mutations, obs) => {
                        var links = Array.from(suggestionsDiv.querySelectorAll('a'));
                        var results = links.map(a => a.innerText);
                        obs.disconnect();
                        sendResults(results);
                    });

                    observer.observe(suggestionsDiv, { childList: true, subtree: true, attributes: true });

                    setTimeout(() => {
                        observer.disconnect();
                        var links = Array.from(suggestionsDiv.querySelectorAll('a'));
                        var results = links.map(a => a.innerText);
                        sendResults(results);
                    }, 1000);
                })();
            """;

            await WebView.ExecuteScriptAsync(script);
        }
        catch { }
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not (string pageName, string pageHash, MainPage mainPage)) return;

        PageName = pageName;
        _pageHash = pageHash;
        _parent = mainPage;

        var cacheKey = PageName + _pageHash;

        _parent.ParentWindow.ToggleHomeButton(_parent.BackStackDepth > 0 || pageName != "나무위키:대문");

        WeakReferenceMessenger.Default.Send(new SetAutoSuggestBoxTextMessage(_parent.ParentWindow, PageName));

        var disableCache = App.GlobalPreferenceViewModel.Preference.DisableWebViewCache;

        if (!disableCache && PageCache.TryGetValue(cacheKey, out var cachedPage) && cachedPage != null)
        {
            WebView = cachedPage.WebView;

            if (WebView.Parent is Grid parentGrid)
            {
                parentGrid.Children.Remove(WebView);
            }

            await UpdateScrollBarVisibilityAsync(App.GlobalPreferenceViewModel.Preference);
            await UpdateAdsVisibilityAsync(App.GlobalPreferenceViewModel.Preference);
            await UpdateFontScaleAsync(App.GlobalPreferenceViewModel.Preference);

            WebViewContainer.Content = WebView;
        }
        else
        {
            _isFirstNavigation = true;

            WebView = new WebView2 { Visibility = Visibility.Collapsed };
            WebViewContainer.Content = WebView;

            if (!disableCache)
            {
                PageCache[cacheKey] = this;

                // Track WebView keys for the parent MainWindow (to prevent memory leaks)
                if (!MainWindowWebViewKeys.ContainsKey(_parent.ParentWindow)) MainWindowWebViewKeys[_parent.ParentWindow] = [];
                MainWindowWebViewKeys[_parent.ParentWindow].Add(cacheKey);
            }

            await WebView.EnsureCoreWebView2Async();
            WebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            WebView.Source = new Uri(Constants.BaseUrl + PageName);

            if (PageName != "나무위키:대문" && App.GlobalPreferenceViewModel.UsePageHistory)
                App.GlobalPreferenceViewModel.PageHistories.Add(new(PageName));
        }

        WebView.CoreWebView2.DOMContentLoaded += OnDOMContentLoaded;
        WebView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
        WebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        WebView.CoreWebView2.NavigationStarting += OnNavigationStarting;
        WebView.CoreWebView2.ContextMenuRequested += OnContextMenuRequested;

        TrimBackStack(cacheKey);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        var cacheKey = PageName + _pageHash;

        if (WebView != null)
        {
            WebView.CoreWebView2.DOMContentLoaded -= OnDOMContentLoaded;
            WebView.CoreWebView2.NewWindowRequested -= OnNewWindowRequested;
            WebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            WebView.CoreWebView2.NavigationStarting -= OnNavigationStarting;
            WebView.CoreWebView2.ContextMenuRequested -= OnContextMenuRequested;
            WebViewContainer.Content = null;

            if (App.GlobalPreferenceViewModel.Preference.DisableWebViewCache)
            {
                WebView.Close();
                WebView = null;
            }
        }

        if (e.NavigationMode == NavigationMode.Back)
        {
            PageCache.Remove(cacheKey);
            if (MainWindowWebViewKeys.TryGetValue(_parent.ParentWindow, out var keys)) keys.Remove(cacheKey);
            WebView?.Close();
        }
    }

    public async void OnContextMenuRequested(CoreWebView2 sender, CoreWebView2ContextMenuRequestedEventArgs args)
    {
        args.Handled = true;

        if (args.ContextMenuTarget.LinkUri?.StartsWith(Constants.BaseUrl) == true)
        {
            var pageName = HttpUtility.UrlDecode(args.ContextMenuTarget.LinkUri[Constants.BaseUrl.Length..]);

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

    public async void OnDOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
    {
        // Initial setup after DOM is loaded
        await HideHeaderAsync();
        await InjectNavigationInterceptScriptAsync();
        await UpdateTimestampAsync();

        // Update DOM based on preferences
        await UpdateScrollBarVisibilityAsync(App.GlobalPreferenceViewModel.Preference);
        await UpdateAdsVisibilityAsync(App.GlobalPreferenceViewModel.Preference);
        await UpdateFontScaleAsync(App.GlobalPreferenceViewModel.Preference);

        WebView.Visibility = Visibility.Visible;
    }

    public async void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var message = args.TryGetWebMessageAsString();

        if (message != null && message.StartsWith('{'))
        {
            try
            {
                if (_autoSuggestCts?.IsCancellationRequested == true) return;

                var node = System.Text.Json.Nodes.JsonNode.Parse(message);
                if (node?["type"]?.GetValue<string>() == "suggestion")
                {
                    var query = node["query"]?.GetValue<string>();
                    if (query == _lastQueryText)
                    {
                        var items = node["items"]?.AsArray();
                        var newItems = new List<string>();
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                var text = item?.GetValue<string>();
                                if (!string.IsNullOrWhiteSpace(text))
                                    newItems.Add(text);
                            }
                        }

                        WeakReferenceMessenger.Default.Send(new AutoSuggestBoxItemsSourceMessage(_parent.ParentWindow, newItems));
                    }
                }
            }
            catch { }
            return;
        }

        await ProcessUrlAsync(message);
    }

    public async void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (args.IsRedirected) return;

        var url = args.Uri;

        if (_isFirstNavigation)
        {
            _isFirstNavigation = false;
            return;
        }

        args.Cancel = true;


        await ProcessUrlAsync(url);
    }

    public static void OnNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args) => args.Handled = true;

    private async void OnAddPendingPageButtonClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PendingPageNameGrid.Visibility = Visibility.Collapsed;

        var pageName = _pendingPageName;
        if (string.IsNullOrEmpty(pageName))
        {
            await MessageBox.ShowErrorAsync(true, _parent.ParentWindow, "잘못된 페이지 이름입니다.", "오류");
            return;
        }

        var alreadyExists = App.GlobalPreferenceViewModel.PendingPages.Any(p => p.PageName == pageName);
        if (alreadyExists)
        {
            var result = await MessageBox.ShowAsync(true, _parent.ParentWindow, "이미 추가된 페이지입니다. 그래도 추가하시겠습니까?", "경고", MessageBoxButtons.YesNo);
            if (result == MessageBoxResult.NO) return;
        }

        var pendingPage = new PendingPage(pageName);
        App.GlobalPreferenceViewModel.PendingPages.Add(pendingPage);

        WeakReferenceMessenger.Default.Send(new PendingPageAddedMessage(pendingPage));
    }

    private async void OnPreferenceChanged(object recipient, ValueChangedMessage<Preference> message)
    {
        var newPreference = message.Value;
        await UpdateScrollBarVisibilityAsync(newPreference);
        await UpdateAdsVisibilityAsync(newPreference);
    }

    private async void OnAutoSuggestBoxQuerySubmittedMessageReceived(object recipient, AutoSuggestBoxQuerySubmittedMessage message)
    {
        if (!IsActive) return;
        if (message.MainWindow != _parent.ParentWindow) return;

        var pageName = message.Value;
        await ValidateAndNavigateAsync(pageName);
    }

    private async void OnAutoSuggestBoxTextChangedMessageReceived(object recipient, AutoSuggestBoxTextChangedMessage message)
    {
        if (!IsActive) return;
        if (message.MainWindow != _parent.ParentWindow) return;

        var (text, reason) = message.Value;
        if (reason != AutoSuggestionBoxTextChangeReason.UserInput) return;

        _lastQueryText = text;
        _autoSuggestCts?.Cancel();
        _autoSuggestCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _autoSuggestCts.Token);
            await QueryAutoSuggestionAsync(text);
        }
        catch (TaskCanceledException) { }
    }

    private async void OnFontScaleChangedMessageReceived(object recipient, FontScaleChangedMessage message) => await UpdateFontScaleAsync(App.GlobalPreferenceViewModel.Preference);

    private async void OnRandomPageRequestedMessageReceived(object recipient, RandomPageRequestedMessage message)
    {
        if (!IsActive) return;
        if (message.MainWindow != _parent.ParentWindow) return;

        _parent.ParentWindow.ShowLoading("랜덤 페이지를 불러오는 중...");
        try
        {
            var request = new RestRequest("/random", Method.Get);
            var response = await s_client.ExecuteAsync(request);

            if (response.ResponseUri != null)
            {
                var pageName = HttpUtility.UrlDecode(response.ResponseUri.AbsolutePath.Replace("/w/", ""));
                _parent.NavigateToPage(pageName);
            }
            else
            {
                await MessageBox.ShowErrorAsync(true, _parent.ParentWindow, "랜덤 페이지를 불러오는데 실패했습니다.", "오류");
            }
        }
        finally
        {
            _parent.ParentWindow.HideLoading();
        }
    }
}
