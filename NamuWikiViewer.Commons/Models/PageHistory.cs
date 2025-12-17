using System;
using System.Collections.Generic;
using System.Text;

namespace NamuWikiViewer.Commons.Models;

public class PageHistory()
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string PageName { get; set; }

    // For (de)serialization, parameterless constructor is needed
    public PageHistory(string pageName) : this()
    {
        PageName = pageName;
    }
}
