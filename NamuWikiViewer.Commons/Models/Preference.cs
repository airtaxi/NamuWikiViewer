namespace NamuWikiViewer.Commons.Models;

public class Preference
{
    public bool HideWebViewScrollBar { get; set; } = false;

    public bool BlockAds { get; set; } = false;

    public List<PendingPage> PendingPages { get; set; } = [];
}
