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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace NamuWikiViewer.Windows.Controls;

public sealed partial class SettingsDialog : ContentDialog
{
    public SettingsDialog()
    {
        InitializeComponent();

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
}
