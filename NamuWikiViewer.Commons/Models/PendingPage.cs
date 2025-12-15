using System;
using System.Collections.Generic;
using System.Text;

namespace NamuWikiViewer.Commons.Models
{
    public class PendingPage()
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string PageName { get; set; }

        // For (de)serialization, parameterless constructor is needed
        public PendingPage(string pageName) : this()
        {
            PageName = pageName;
        }
    }
}
