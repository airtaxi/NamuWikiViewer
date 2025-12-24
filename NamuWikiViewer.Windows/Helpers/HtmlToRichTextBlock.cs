using HtmlAgilityPack;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Web;
using Microsoft.UI;
using Windows.UI.Text; // For Colors

namespace NamuWikiViewer.Windows.Helpers;

public static class HtmlToRichTextBlock
{
    public static void Convert(string html, RichTextBlock richTextBlock, Action<string> onInternalLinkClick = null, Action<string> onFootnoteClick = null)
    {
        richTextBlock.Blocks.Clear();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var paragraph = new Paragraph();
        foreach (var node in doc.DocumentNode.ChildNodes)
        {
            ProcessNode(node, paragraph.Inlines, onInternalLinkClick, onFootnoteClick);
        }
        richTextBlock.Blocks.Add(paragraph);
    }

    private static void ProcessNode(HtmlNode node, InlineCollection inlines, Action<string> onInternalLinkClick, Action<string> onFootnoteClick)
    {
        switch (node.Name)
        {
            case "#text":
                var text = HttpUtility.HtmlDecode(node.InnerText);
                if (!string.IsNullOrEmpty(text))
                    inlines.Add(new Run { Text = text });
                break;
            case "a":
                var href = node.GetAttributeValue("href", "");

                var hyperlink = new Hyperlink();
                string fullUrl = href;

                if (!string.IsNullOrEmpty(href))
                {
                    if (href.StartsWith("//")) fullUrl = "https:" + href;
                    else if (href.StartsWith('/')) fullUrl = "https://namu.wiki" + href;
                }

                bool isInternal = !string.IsNullOrEmpty(fullUrl) && fullUrl.StartsWith("https://namu.wiki/w/");
                bool isFootnote = href.StartsWith("#fn-");
                bool isReverseFootnote = href.StartsWith("#rfn-");

                if (isInternal)
                {
                    hyperlink.Foreground = new SolidColorBrush(Colors.DarkOrange);
                    hyperlink.UnderlineStyle = UnderlineStyle.Single;

                    var pageName = fullUrl["https://namu.wiki/w/".Length..];
                    pageName = HttpUtility.UrlDecode(pageName);

                    hyperlink.Click += (s, e) =>
                    {
                        onInternalLinkClick?.Invoke(pageName);
                    };
                }
                else if (isFootnote || isReverseFootnote)
                {
                    hyperlink.Foreground = new SolidColorBrush(Colors.DarkOrange);
                    hyperlink.UnderlineStyle = UnderlineStyle.Single;

                    hyperlink.Click += (s, e) =>
                    {
                        var id = href[1..]; // Remove '#'
                        id = HttpUtility.UrlDecode(id);
                        onFootnoteClick?.Invoke(id);
                    };
                }
                else
                {
                    if (!string.IsNullOrEmpty(fullUrl))
                    {
                        try
                        {
                            hyperlink.NavigateUri = new Uri(fullUrl);
                        }
                        catch { }
                    }

                    // Check if external (not namu.wiki)
                    if (!string.IsNullOrEmpty(fullUrl) && !fullUrl.StartsWith("https://namu.wiki"))
                    {
                        hyperlink.Foreground = new SolidColorBrush(Colors.Green);
                        hyperlink.FontWeight = Microsoft.UI.Text.FontWeights.Bold;
                        hyperlink.UnderlineStyle = UnderlineStyle.Single;
                    }
                }

                foreach (var child in node.ChildNodes)
                {
                    ProcessNode(child, hyperlink.Inlines, onInternalLinkClick, onFootnoteClick);
                }

                if (isInternal)
                {
                    var pageName = fullUrl["https://namu.wiki/w/".Length..];
                    pageName = HttpUtility.UrlDecode(pageName);
                    
                    var linkText = HttpUtility.HtmlDecode(node.InnerText).Trim();
                    // Check if text is different and no images are present
                    if (!string.IsNullOrEmpty(linkText) && 
                        !string.Equals(linkText, pageName, StringComparison.OrdinalIgnoreCase) &&
                        !node.Descendants("img").Any())
                    {
                        hyperlink.Inlines.Add(new Run { Text = $"({pageName})" });
                    }
                }

                inlines.Add(hyperlink);
                break;
            case "img":
                var src = node.GetAttributeValue("data-src", "");
                if (string.IsNullOrEmpty(src))
                {
                    src = node.GetAttributeValue("src", "");
                }

                if (!string.IsNullOrEmpty(src))
                {
                    // Skip data URIs (usually placeholders)
                    if (src.StartsWith("data:")) return;

                    if (src.StartsWith("//")) src = "https:" + src;
                    
                    try
                    {
                        var image = new Image
                        {
                            Source = new BitmapImage(new Uri(src)),
                            Stretch = Stretch.Uniform,
                            MaxWidth = 300,
                            Margin = new Thickness(0, 10, 0, 10)
                        };
                        var container = new InlineUIContainer { Child = image };
                        inlines.Add(container);
                    }
                    catch { }
                }
                break;
            case "noscript":
                // Ignore noscript content to prevent duplicate images
                break;
            case "b":
            case "strong":
                var boldSpan = new Span { FontWeight = Microsoft.UI.Text.FontWeights.Bold };
                foreach (var child in node.ChildNodes) ProcessNode(child, boldSpan.Inlines, onInternalLinkClick, onFootnoteClick);
                inlines.Add(boldSpan);
                break;
            case "u":
                var underlineSpan = new Span { TextDecorations = TextDecorations.Underline };
                foreach (var child in node.ChildNodes) ProcessNode(child, underlineSpan.Inlines, onInternalLinkClick, onFootnoteClick);
                inlines.Add(underlineSpan);
                break;
            case "s":
            case "del":
                var strikeSpan = new Span { TextDecorations = TextDecorations.Strikethrough };
                foreach (var child in node.ChildNodes) ProcessNode(child, strikeSpan.Inlines, onInternalLinkClick, onFootnoteClick);
                inlines.Add(strikeSpan);
                break;
            case "br":
                inlines.Add(new LineBreak());
                break;
            default:
                if (node.HasChildNodes)
                {
                    foreach (var child in node.ChildNodes)
                    {
                        ProcessNode(child, inlines, onInternalLinkClick, onFootnoteClick);
                    }
                }
                break;
        }
    }
}
