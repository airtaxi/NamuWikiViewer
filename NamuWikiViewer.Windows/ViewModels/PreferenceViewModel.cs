using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using NamuWikiViewer.Windows.Models;
using SocialServiceWorkerServiceRecordAutomation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamuWikiViewer.Windows.ViewModels;

public partial class PreferenceViewModel : ObservableObject
{
    private readonly Preference _preference;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SavePreferenceCommand))]
    public partial ScrollBarVisibility WebViewVerticalScrollBarVisibility { get; set; }

    public PreferenceViewModel()
    {
        _preference = Configuration.GetValue<Preference>("Preference") ?? new();

        // Initialize properties
        WebViewVerticalScrollBarVisibility = _preference.WebViewVerticalScrollBarVisibility;
    }

    [RelayCommand]
    public void SavePreference()
    {
        // Update preference model
        _preference.WebViewVerticalScrollBarVisibility = WebViewVerticalScrollBarVisibility;

        Configuration.SetValue("Preference", _preference);
    }
}
