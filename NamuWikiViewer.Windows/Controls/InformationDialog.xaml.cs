using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using NamuWikiViewer.Commons.Models;
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

        ApplyTheme(App.GlobalPreferenceViewModel.Preference.Theme);

        WeakReferenceMessenger.Default.Register<ValueChangedMessage<Preference>>(this, OnPreferenceChanged);

        Closed += (s, e) => WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private void OnPreferenceChanged(object recipient, ValueChangedMessage<Preference> message)
    {
        ApplyTheme(message.Value.Theme);
    }

    private void ApplyTheme(AppTheme theme)
    {
        RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
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
