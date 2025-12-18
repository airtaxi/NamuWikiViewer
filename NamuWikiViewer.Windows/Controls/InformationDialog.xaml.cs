using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace NamuWikiViewer.Windows.Controls;

public sealed partial class InformationDialog : ContentDialog
{
    public InformationDialog()
    {
        InitializeComponent();
        VersionTextBlock.Text = GetAppVersion();
    }

    private static string GetAppVersion()
    {
        try
        {
            var version = Package.Current.Id.Version;
            return $"v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
        catch
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return $"v{version?.Major ?? 0}.{version?.Minor ?? 0}.{version?.Build ?? 0}.{version?.Revision ?? 0}";
        }
    }
}
