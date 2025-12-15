using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace NamuWikiViewer.Windows.Pages;

public sealed partial class BrowserPage : Page
{
    private const string BaseUrl = "https://namu.wiki/w/";
    private readonly string _pageName;

    public BrowserPage()
    {
        InitializeComponent();
        _pageName = "나무위키:대문";
    }

    public BrowserPage(string pageName) : this()
    {
        _pageName = pageName;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        await MainWebView.EnsureCoreWebView2Async();
        if (e.NavigationMode == NavigationMode.New) MainWebView.Source = new Uri(BaseUrl + _pageName);
    }
}
