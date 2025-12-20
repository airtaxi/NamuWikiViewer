namespace NamuWikiViewer.Commons.Models;

public class Preference
{
    public bool UsePageHistory { get; set; } = true;

    public bool HideWebViewScrollBar { get; set; } = false;

    public bool BlockAds { get; set; } = false;

    public bool HideNamuNewsCard { get; set; } = false;

    public bool HideRecentChangesCard { get; set; } = false;

    public bool HideRelatedSearchCard { get; set; } = false;

    public AppTheme Theme { get; set; } = AppTheme.System;

    public bool DisableWebViewCache { get; set; } = false;

    public List<PendingPage> PendingPages { get; set; } = [];

    public List<PageHistory> PageHistories { get; set; } = [];

    public double FontScale { get; set; } = 1.0;

    public int? BackStackDepthLimit { get; set; } = 10;
}
