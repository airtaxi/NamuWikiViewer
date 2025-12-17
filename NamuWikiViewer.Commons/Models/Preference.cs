namespace NamuWikiViewer.Commons.Models;

public class Preference
{
    public bool UsePageHistory { get; set; } = true;

    public bool HideWebViewScrollBar { get; set; } = false;

    public bool BlockAds { get; set; } = false;

    public List<PendingPage> PendingPages { get; set; } = [];

    public List<PageHistory> PageHistories { get; set; } = [];
}
